using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Google.Api;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Logging.Type;
using Google.Cloud.Logging.V2;
using Google.Protobuf.WellKnownTypes;
using Grpc.Auth;
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
        private readonly bool _errorReportingEnabled;

        public GoogleCloudLoggingSink(GoogleCloudLoggingSinkOptions sinkOptions, MessageTemplateTextFormatter messageTemplateTextFormatter, int batchSizeLimit, TimeSpan period)
            : base(batchSizeLimit, period)
        {
            if (sinkOptions.GoogleCredentialJson == null)
            {
                _client = LoggingServiceV2Client.Create();
            }
            else
            {
                var googleCredential = GoogleCredential.FromJson(sinkOptions.GoogleCredentialJson);
                var channel = new Grpc.Core.Channel(LoggingServiceV2Client.DefaultEndpoint.Host, googleCredential.ToChannelCredentials());
                _client = LoggingServiceV2Client.Create(channel);
            }

            _sinkOptions = sinkOptions;

            _resource = new MonitoredResource { Type = sinkOptions.ResourceType };
            foreach (var kvp in _sinkOptions.ResourceLabels)
                _resource.Labels[kvp.Key] = kvp.Value;

            var ln = new LogName(sinkOptions.ProjectId, sinkOptions.LogName);
            _logName = ln.ToString();
            _logNameToWrite = LogNameOneof.From(ln);

            _messageTemplateTextFormatter = messageTemplateTextFormatter;

            _errorReportingEnabled = !string.IsNullOrWhiteSpace(_sinkOptions.ErrorReportingServiceName);
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

                    if (e.Exception != null && _errorReportingEnabled && (entry.Severity == LogSeverity.Error || entry.Severity == LogSeverity.Critical))
                    {
                        jsonStruct.Fields.Add("message", Value.ForString(RenderEventMessage(e) + "\n" + RenderException(e.Exception)));

                        var serviceContextStruct = new Struct();
                        jsonStruct.Fields.Add("serviceContext", Value.ForStruct(serviceContextStruct));

                        serviceContextStruct.Fields.Add("service", Value.ForString(_sinkOptions.ErrorReportingServiceName));
                        serviceContextStruct.Fields.Add("version", Value.ForString(_sinkOptions.ErrorReportingServiceVersion));
                    }
                    else
                    {
                        jsonStruct.Fields.Add("message", Value.ForString(RenderEventMessage(e)));
                    }

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

        private static string RenderException(Exception ex)
        {
            if (ex.GetType() == typeof(AggregateException))
            {
                // ErrorReporting won't report all InnerExceptions for an AggregateException. This work-around isn't perfect but better than the default behavior
                var MessageStrings = new List<string>();
                MessageStrings.AddRange(((AggregateException)ex).Flatten().InnerExceptions.Select(s => s.Message));
                MessageStrings.Add(ex.StackTrace);

                return String.Join("\n", MessageStrings);
            }
            else
            {
                return ex.ToString();
            }
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

            return e.RenderMessage();
        }

        /// <summary>
        /// Writes event properties as a JSON object for a GCP log entry.
        /// </summary>
        private void WritePropertyAsJson(LogEntry log, Struct jsonStruct, string propertyKey, LogEventPropertyValue propertyValue)
        {
            switch (propertyValue)
            {
                case ScalarValue scalarValue when scalarValue.Value is null:
                    jsonStruct.Fields.Add(propertyKey, Value.ForNull());
                    break;

                case ScalarValue scalarValue when scalarValue.Value is bool boolValue:
                    jsonStruct.Fields.Add(propertyKey, Value.ForBool(boolValue));
                    break;

                case ScalarValue scalarValue
                    when scalarValue.Value is short || scalarValue.Value is ushort || scalarValue.Value is int
                         || scalarValue.Value is uint || scalarValue.Value is long || scalarValue.Value is ulong
                         || scalarValue.Value is float || scalarValue.Value is double || scalarValue.Value is decimal:

                    // all numbers are converted to double and may lose precision
                    // numbers should be sent as strings if they do not fit in a double
                    jsonStruct.Fields.Add(propertyKey, Value.ForNumber(Convert.ToDouble(scalarValue.Value)));
                    break;

                case ScalarValue scalarValue when scalarValue.Value is string stringValue:
                    jsonStruct.Fields.Add(propertyKey, Value.ForString(stringValue));
                    CheckIfSourceContext(log, propertyKey, stringValue);
                    break;

                case ScalarValue scalarValue:
                    // handle all other scalar values as strings
                    var strValue = scalarValue.Value.ToString();
                    jsonStruct.Fields.Add(propertyKey, Value.ForString(strValue));
                    CheckIfSourceContext(log, propertyKey, strValue);
                    break;

                case SequenceValue sequenceValue:
                    var sequenceChild = new Struct();
                    for (var i = 0; i < sequenceValue.Elements.Count; i++)
                        WritePropertyAsJson(log, sequenceChild, i.ToString(), sequenceValue.Elements[i]);

                    jsonStruct.Fields.Add(propertyKey, Value.ForList(sequenceChild.Fields.Values.ToArray()));
                    break;

                case StructureValue structureValue:
                    var structureChild = new Struct();
                    foreach (var childProperty in structureValue.Properties)
                        WritePropertyAsJson(log, structureChild, childProperty.Name, childProperty.Value);

                    jsonStruct.Fields.Add(propertyKey, Value.ForStruct(structureChild));
                    break;

                case DictionaryValue dictionaryValue:
                    var dictionaryChild = new Struct();
                    foreach (var childProperty in dictionaryValue.Elements)
                        WritePropertyAsJson(log, dictionaryChild, childProperty.Key.Value?.ToString(), childProperty.Value);

                    jsonStruct.Fields.Add(propertyKey, Value.ForStruct(dictionaryChild));
                    break;
            }
        }

        /// <summary>
        /// Writes event properties as labels for a GCP log entry.
        /// GCP log labels are a flat key/value namespace so all child event properties will be prefixed with parent property names "parentkey.childkey" similar to json path.
        /// </summary>
        private void WritePropertyAsLabel(LogEntry log, string propertyKey, LogEventPropertyValue propertyValue)
        {
            switch (propertyValue)
            {
                case ScalarValue scalarValue when scalarValue.Value is null:
                    log.Labels.Add(propertyKey, String.Empty);
                    break;

                case ScalarValue scalarValue:
                    var stringValue = scalarValue.Value.ToString();
                    log.Labels.Add(propertyKey, stringValue);
                    CheckIfSourceContext(log, propertyKey, stringValue);
                    break;

                case SequenceValue sequenceValue:
                    log.Labels.Add(propertyKey, String.Join(",", sequenceValue.Elements));
                    break;

                case StructureValue structureValue when structureValue.Properties.Count > 0:
                    foreach (var childProperty in structureValue.Properties)
                        WritePropertyAsLabel(log, propertyKey + "." + childProperty.Name, childProperty.Value);

                    break;

                case DictionaryValue dictionaryValue when dictionaryValue.Elements.Count > 0:
                    foreach (var childProperty in dictionaryValue.Elements)
                        WritePropertyAsLabel(log, propertyKey + "." + childProperty.Key.ToString().Replace("\"", ""), childProperty.Value);

                    break;
            }
        }

        private void CheckIfSourceContext(LogEntry log, string propertyKey, string stringValue)
        {
            if (_sinkOptions.UseSourceContextAsLogName && propertyKey.Equals("SourceContext", StringComparison.OrdinalIgnoreCase))
                log.LogName = new LogName(_sinkOptions.ProjectId, stringValue).ToString();
        }

        private static LogSeverity TranslateSeverity(LogEventLevel level)
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
