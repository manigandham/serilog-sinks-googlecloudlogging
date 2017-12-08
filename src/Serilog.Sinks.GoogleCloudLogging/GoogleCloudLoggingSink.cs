using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Api;
using Google.Cloud.Logging.Type;
using Google.Cloud.Logging.V2;
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
                    Timestamp = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(e.Timestamp)
                };

                if (_messageTemplateTextFormatter != null)
                {
                    using (var stringWriter = new StringWriter())
                    {
                        _messageTemplateTextFormatter.Format(e, stringWriter);
                        entry.TextPayload = stringWriter.ToString();
                    }
                }
                else
                {
                    entry.TextPayload = e.RenderMessage();
                }

                WriteProperties(entry, e.Properties);

                logEntries.Add(entry);
            }

            await _client.WriteLogEntriesAsync(_logNameToWrite, _resource, _sinkOptions.Labels, logEntries);
        }

        /// <summary>
        /// Writes event properties as labels for GCP log entry.
        /// GCP log labels are a flat key/value namespace so all child event properties will be prefixed with parent property names "parentkey.childkey" similar to json path.
        /// Scalar and sequence values will be written even if values are empty so that key names are still logged as labels.
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="properties"></param>
        private void WriteProperties(LogEntry entry, IReadOnlyDictionary<string, LogEventPropertyValue> properties)
        {
            foreach (var property in properties)
            {
                switch (property.Value)
                {
                    case ScalarValue scalarValue:
                        {
                            // google cloud logging does not support null values, will write empty string instead to preserve keys as labels
                            var value = scalarValue.Value?.ToString() ?? string.Empty;
                            entry.Labels.Add(property.Key, value);

                            if (_sinkOptions.UseSourceContextAsLogName && property.Key.Equals("SourceContext", StringComparison.OrdinalIgnoreCase))
                                entry.LogName = new LogName(_sinkOptions.ProjectId, value).ToString();

                            break;
                        }
                    case SequenceValue sequenceValue:
                        {
                            var value = String.Join(",", sequenceValue.Elements);
                            entry.Labels.Add(property.Key, value);
                            break;
                        }
                    case StructureValue structureValue when structureValue.Properties.Count > 0:
                        {
                            var dict = new Dictionary<string, LogEventPropertyValue>(structureValue.Properties.Count);
                            foreach (var childProperty in structureValue.Properties)
                                dict[property.Key + "." + childProperty.Name] = childProperty.Value;

                            WriteProperties(entry, dict);
                            break;
                        }
                    case DictionaryValue dictionaryValue when dictionaryValue.Elements.Count > 0:
                        {
                            var dict = new Dictionary<string, LogEventPropertyValue>(dictionaryValue.Elements.Count);
                            foreach (var kv in dictionaryValue.Elements)
                                dict[property.Key + "." + kv.Key.ToString().Replace("\"", string.Empty)] = kv.Value;

                            WriteProperties(entry, dict);
                            break;
                        }
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
