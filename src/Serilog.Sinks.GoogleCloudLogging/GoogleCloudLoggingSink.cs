using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Google.Api;
using Google.Cloud.Logging.Type;
using Google.Cloud.Logging.V2;
using Google.Protobuf.WellKnownTypes;
using Serilog.Events;
using Serilog.Formatting.Display;
using Serilog.Sinks.PeriodicBatching;

namespace Serilog.Sinks.GoogleCloudLogging
{
    public class GoogleCloudLoggingSink : PeriodicBatchingSink
    {
        private readonly GoogleCloudLoggingSinkOptions _sinkOptions;
        private readonly LoggingServiceV2Client _client;
        private readonly string _logName;
        private readonly LogNameOneof _logNameToWrite;
        private readonly MonitoredResource _resource;
        private readonly MessageTemplateTextFormatter _messageTemplateTextFormatter;

        public GoogleCloudLoggingSink(GoogleCloudLoggingSinkOptions sinkOptions, MessageTemplateTextFormatter messageTemplateTextFormatter, int batchSizeLimit, TimeSpan period) : base(batchSizeLimit, period)
        {
            _client = LoggingServiceV2Client.Create();
            _sinkOptions = sinkOptions;

            _resource = new MonitoredResource { Type = sinkOptions.ResourceType };
            var ln = new LogName(sinkOptions.ProjectId, sinkOptions.LogName);
            _logName = ln.ToString();
            _logNameToWrite = LogNameOneof.From(ln);

            _messageTemplateTextFormatter = messageTemplateTextFormatter;
        }

        protected override async Task EmitBatchAsync(IEnumerable<LogEvent> events)
        {
            var logEntries = new List<LogEntry>();

            foreach (var e in events)
            {
                var entry = new LogEntry
                {
                    LogName = _logName,
                    Severity = TranslateSeverity(e.Level),
                    Timestamp = Timestamp.FromDateTimeOffset(e.Timestamp)
                };

                if (_sinkOptions.UseJsonOutput)
                {
                    var jsonStruct = new Struct();
                    jsonStruct.Fields.Add("message", Value.ForString(RenderEventMessage(e)));

                    var propertiesStruct = new Struct();
                    jsonStruct.Fields.Add("properties", Value.ForStruct(propertiesStruct));

                    foreach (var property in e.Properties)
                        WritePropertyAsJson(entry, propertiesStruct, property.Key, property.Value);

                    entry.JsonPayload = jsonStruct;
                }
                else
                {
                    entry.TextPayload = RenderEventMessage(e);

                    foreach (var property in e.Properties)
                        WritePropertyAsLabel(entry, property.Key, property.Value);
                }

                logEntries.Add(entry);
            }

            await _client.WriteLogEntriesAsync(_logNameToWrite, _resource, _sinkOptions.Labels, logEntries);
        }

        private string RenderEventMessage(LogEvent e)
        {
            if (_messageTemplateTextFormatter != null)
            {
                using (var stringWriter = new StringWriter())
                {
                    _messageTemplateTextFormatter.Format(e, stringWriter);
                    return stringWriter.ToString();
                }
            }
            else
            {
                return e.RenderMessage();
            }
        }

        /// <summary>
        /// Writes event properties as a JSON object for a GCP log entry.
        /// Scalar and sequence properties will be written even if values are empty so that key names are still logged.
        /// </summary>
        private void WritePropertyAsJson(LogEntry log, Struct jsonStruct, string propertyKey, LogEventPropertyValue propertyValue)
        {
            switch (propertyValue)
            {
                case ScalarValue scalarValue when scalarValue.Value is string:
                    {
                        var stringValue = scalarValue.Value?.ToString() ?? "";
                        jsonStruct.Fields.Add(propertyKey, Value.ForString(stringValue));

                        if (_sinkOptions.UseSourceContextAsLogName && propertyKey.Equals("SourceContext", StringComparison.OrdinalIgnoreCase))
                            log.LogName = new LogName(_sinkOptions.ProjectId, stringValue).ToString();

                        break;
                    }
                case ScalarValue scalarValue when Double.TryParse(scalarValue.Value?.ToString() ?? "", out var doubleValue):
                    {
                        jsonStruct.Fields.Add(propertyKey, Value.ForNumber(doubleValue));

                        break;
                    }
                case SequenceValue sequenceValue:
                    {
                        var childStruct = new Struct();
                        for (int i = 0; i < sequenceValue.Elements.Count; i++)
                            WritePropertyAsJson(log, childStruct, i.ToString(), sequenceValue.Elements[i]);

                        jsonStruct.Fields.Add(propertyKey, Value.ForList(childStruct.Fields.Values.ToArray()));

                        break;
                    }
                case StructureValue structureValue:
                    {
                        var childStruct = new Struct();
                        foreach (var childProperty in structureValue.Properties)
                            WritePropertyAsJson(log, childStruct, childProperty.Name, childProperty.Value);

                        jsonStruct.Fields.Add(propertyKey, Value.ForStruct(childStruct));

                        break;
                    }
                case DictionaryValue dictionaryValue:
                    {
                        var childStruct = new Struct();
                        foreach (var childProperty in dictionaryValue.Elements)
                            WritePropertyAsJson(log, childStruct, childProperty.Key.Value?.ToString(), childProperty.Value);

                        jsonStruct.Fields.Add(propertyKey, Value.ForStruct(childStruct));

                        break;
                    }
            }
        }


        /// <summary>
        /// Writes event properties as labels for a GCP log entry.
        /// GCP log labels are a flat key/value namespace so all child event properties will be prefixed with parent property names "parentkey.childkey" similar to json path.
        /// Scalar and sequence properties will be written even if values are empty so that key names are still logged.
        /// </summary>
        private void WritePropertyAsLabel(LogEntry log, string propertyKey, LogEventPropertyValue propertyValue)
        {
            switch (propertyValue)
            {
                case ScalarValue scalarValue:
                    {
                        var stringValue = scalarValue.Value?.ToString() ?? "";
                        log.Labels.Add(propertyKey, stringValue);

                        if (_sinkOptions.UseSourceContextAsLogName && propertyKey.Equals("SourceContext", StringComparison.OrdinalIgnoreCase))
                            log.LogName = new LogName(_sinkOptions.ProjectId, stringValue).ToString();

                        break;
                    }
                case SequenceValue sequenceValue:
                    {
                        log.Labels.Add(propertyKey, String.Join(",", sequenceValue.Elements));

                        break;
                    }
                case StructureValue structureValue when structureValue.Properties.Count > 0:
                    {
                        foreach (var childProperty in structureValue.Properties)
                            WritePropertyAsLabel(log, propertyKey + "." + childProperty.Name, childProperty.Value);

                        break;
                    }
                case DictionaryValue dictionaryValue when dictionaryValue.Elements.Count > 0:
                    {
                        foreach (var childProperty in dictionaryValue.Elements)
                            WritePropertyAsLabel(log, propertyKey + "." + childProperty.Key.ToString().Replace("\"", ""), childProperty.Value);

                        break;
                    }
            }
        }

        private LogSeverity TranslateSeverity(LogEventLevel level)
        {
            switch (level)
            {
                case LogEventLevel.Verbose:
                case LogEventLevel.Debug: return LogSeverity.Debug;
                case LogEventLevel.Information: return LogSeverity.Info;
                case LogEventLevel.Warning: return LogSeverity.Warning;
                case LogEventLevel.Error: return LogSeverity.Error;
                case LogEventLevel.Fatal: return LogSeverity.Critical;
                default: return LogSeverity.Default;
            }
        }
    }
}
