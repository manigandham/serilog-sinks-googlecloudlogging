using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Api;
using Google.Api.Gax;
using Google.Api.Gax.Grpc;
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
        private readonly MonitoredResource _resource;
        private readonly bool _serviceNameAvailable;
        private readonly LogFormatter _logFormatter;
        
        public GoogleCloudLoggingSink(GoogleCloudLoggingSinkOptions sinkOptions, MessageTemplateTextFormatter messageTemplateTextFormatter, int batchSizeLimit, TimeSpan period)
            : base(batchSizeLimit, period)
        {
            _sinkOptions = sinkOptions;

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

            // retrieve current environment details (gke/gce/appengine) from google libraries automatically
            // or fallback to "Global" resource
            var platform = Platform.Instance();

            _resource = platform.Type == PlatformType.Unknown
                ? MonitoredResourceBuilder.GlobalResource
                : MonitoredResourceBuilder.FromPlatform(platform);

            foreach (var kvp in _sinkOptions.ResourceLabels)
                _resource.Labels[kvp.Key] = kvp.Value;

            // use explicit ResourceType if set
            _resource.Type = sinkOptions.ResourceType ?? _resource.Type;

            // use explicit project ID or fallback to project ID found in platform environment details above
            var projectId = _sinkOptions.ProjectId ?? _resource.Labels["project_id"];
            _logName = LogFormatter.CreateLogName(projectId, sinkOptions.LogName);

            _serviceNameAvailable = !String.IsNullOrWhiteSpace(_sinkOptions.ServiceName);
            _logFormatter = new LogFormatter(projectId, _sinkOptions.UseSourceContextAsLogName, messageTemplateTextFormatter);
        }

        protected override async Task EmitBatchAsync(IEnumerable<LogEvent> events)
        {
            using var writer = new StringWriter();
            var entries = events.Select(e => CreateLogEntry(e, writer)).ToList();
            if (entries.Count > 0)
                await _client.WriteLogEntriesAsync((LogNameOneof) null, _resource, _sinkOptions.Labels, entries, CancellationToken.None);
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
                // json output builds up a protobuf object to be serialized in stackdriver logs
                entry.JsonPayload = new Struct();
                entry.JsonPayload.Fields.Add("message", Value.ForString(_logFormatter.RenderEventMessage(e, writer)));

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
                entry.TextPayload = _logFormatter.RenderEventMessage(e, writer);

                foreach (var property in e.Properties)
                    _logFormatter.WritePropertyAsLabel(entry, property.Key, property.Value);
            }

            return entry;
        }

        private static LogSeverity TranslateSeverity(LogEventLevel level) => level switch
        {
            LogEventLevel.Verbose => LogSeverity.Debug,
            LogEventLevel.Debug => LogSeverity.Debug,
            LogEventLevel.Information => LogSeverity.Info,
            LogEventLevel.Warning => LogSeverity.Warning,
            LogEventLevel.Error => LogSeverity.Error,
            LogEventLevel.Fatal => LogSeverity.Critical,
            _ => LogSeverity.Default
        };
    }
}
