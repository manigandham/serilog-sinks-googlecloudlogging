using System;
using System.Collections.Generic;
using System.IO;
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

                foreach (var property in e.Properties)
                {
                    var value = ((ScalarValue) property.Value).Value.ToString();
                    entry.Labels.Add(property.Key, value);

                    if (property.Key.Equals("SourceContext", StringComparison.OrdinalIgnoreCase))
                        entry.LogName = new LogName(_sinkOptions.ProjectId, value).ToString();
                }

                logEntries.Add(entry);
            }

            await _client.WriteLogEntriesAsync(_logNameToWrite, _resource, _sinkOptions.Labels, logEntries);
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
