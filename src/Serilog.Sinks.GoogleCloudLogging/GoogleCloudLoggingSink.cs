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
        private readonly bool _serviceNameAvailable;
        private readonly LogFormatter _logFormatter;

        public GoogleCloudLoggingSink(GoogleCloudLoggingSinkOptions sinkOptions, MessageTemplateTextFormatter messageTemplateTextFormatter, int batchSizeLimit, TimeSpan period)
            : base(batchSizeLimit, period)
        {
            // logging client for google cloud apis
            // requires extra setup if credentials are passed as raw json text
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
            _logFormatter = new LogFormatter(_sinkOptions, messageTemplateTextFormatter);

            _resource = new MonitoredResource { Type = sinkOptions.ResourceType };
            foreach (var kvp in _sinkOptions.ResourceLabels)
                _resource.Labels[kvp.Key] = kvp.Value;

            var ln = new LogName(sinkOptions.ProjectId, sinkOptions.LogName);
            _logName = ln.ToString();
            _logNameToWrite = LogNameOneof.From(ln);

            _serviceNameAvailable = !String.IsNullOrWhiteSpace(_sinkOptions.ServiceName);
        }

        protected override async Task EmitBatchAsync(IEnumerable<LogEvent> events)
        {
            using (var writer = new StringWriter())
            {
                var entries = events.Select(e => CreateLogEntry(e, writer)).ToList();
                if (entries.Count > 0)
                    await _client.WriteLogEntriesAsync(_logNameToWrite, _resource, _sinkOptions.Labels, entries);
            }
        }

        private LogEntry CreateLogEntry(LogEvent e, StringWriter writer)
        {
            var entry = new LogEntry
            {
                LogName = _logName,
                Severity = TranslateSeverity(e.Level),
                Timestamp = Timestamp.FromDateTimeOffset(e.Timestamp)
            };

            if (_sinkOptions.UseJsonOutput)
            {
                // json output builds up a protobuf object that gets serialized in the stackdriver logs
                entry.JsonPayload = new Struct();
                entry.JsonPayload.Fields.Add("message", Value.ForString(_logFormatter.RenderEventMessage(writer, e)));

                var propStruct = new Struct();
                foreach (var property in e.Properties)
                    _logFormatter.WritePropertyAsJson(entry, propStruct, property.Key, property.Value);

                entry.JsonPayload.Fields.Add("properties", Value.ForStruct(propStruct));

                // service name and version are added as extra context data if available
                // these properties are required for any logged exceptions to automatically be picked up by stackdriver error reporting
                if (_serviceNameAvailable)
                {
                    var contextStruct = new Struct();
                    contextStruct.Fields.Add("service", Value.ForString(_sinkOptions.ServiceName));
                    contextStruct.Fields.Add("version", Value.ForString(_sinkOptions.ServiceVersion));
                    entry.JsonPayload.Fields.Add("serviceContext", Value.ForStruct(contextStruct));
                }
            }
            else
            {
                // text output is simple stringification
                entry.TextPayload = _logFormatter.RenderEventMessage(writer, e);

                foreach (var property in e.Properties)
                    _logFormatter.WritePropertyAsLabel(entry, property.Key, property.Value);
            }

            return entry;
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
