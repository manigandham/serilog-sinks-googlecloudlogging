using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Google.Api;
using Google.Api.Gax;
using Google.Api.Gax.Grpc;
using Google.Cloud.Logging.Type;
using Google.Cloud.Logging.V2;
using Google.Protobuf.WellKnownTypes;
using Serilog.Events;
using Serilog.Formatting.Display;
using Serilog.Sinks.PeriodicBatching;

namespace Serilog.Sinks.GoogleCloudLogging
{
    public class GoogleCloudLoggingSink : IBatchedLogEventSink
    {
        private readonly GoogleCloudLoggingSinkOptions _sinkOptions;
        private readonly LoggingServiceV2Client _client;
        private readonly string _logName;
        private readonly MonitoredResource _resource;
        private readonly bool _serviceNameAvailable;
        private readonly LogFormatter _logFormatter;

        public GoogleCloudLoggingSink(GoogleCloudLoggingSinkOptions sinkOptions, MessageTemplateTextFormatter? messageTemplateTextFormatter)
        {
            _sinkOptions = sinkOptions;

            // logging client for google cloud apis
            _client = sinkOptions.GoogleCredentialJson != null
                ? new LoggingServiceV2ClientBuilder { JsonCredentials = sinkOptions.GoogleCredentialJson }.Build()
                : LoggingServiceV2Client.Create();

            // retrieve current environment details (gke/gce/appengine) from google libraries automatically
            var platform = Platform.Instance();

            // resource can be extracted from environment details or fallback to "Global" resource
            _resource = platform.Type == PlatformType.Unknown
                ? MonitoredResourceBuilder.GlobalResource
                : MonitoredResourceBuilder.FromPlatform(platform);
            
            // use explicit ResourceType if set
            _resource.Type = sinkOptions.ResourceType ?? _resource.Type;

            foreach (var kvp in _sinkOptions.ResourceLabels)
                _resource.Labels[kvp.Key] = kvp.Value;

            // use explicit project ID or fallback to project ID found in platform environment details above
            var projectId = _sinkOptions.ProjectId ?? platform.ProjectId ?? _resource.Labels["project_id"];

            _logName = LogFormatter.CreateLogName(projectId, sinkOptions.LogName);
            _logFormatter = new LogFormatter(projectId, _sinkOptions.UseSourceContextAsLogName, _sinkOptions.UseLogCorrelation, messageTemplateTextFormatter);
            _serviceNameAvailable = !String.IsNullOrWhiteSpace(_sinkOptions.ServiceName);
        }

        public Task EmitBatchAsync(IEnumerable<LogEvent> events)
        {
            using var writer = new StringWriter();
            var entries = new List<LogEntry>();
            foreach (var evnt in events)
                entries.Add(CreateLogEntry(evnt, writer));

            if (entries.Count > 0)
                return _client.WriteLogEntriesAsync(_logName, _resource, _sinkOptions.Labels, entries, CancellationToken.None);

            return Task.CompletedTask;
        }

        private LogEntry CreateLogEntry(LogEvent evnt, StringWriter writer)
        {
            var log = new LogEntry
            {
                Severity = TranslateSeverity(evnt.Level),
                Timestamp = Timestamp.FromDateTimeOffset(evnt.Timestamp)
            };

            if (_sinkOptions.UseJsonOutput)
            {
                // json output builds up a protobuf object to be serialized in cloud logging
                var jsonPayload = new Struct();
                jsonPayload.Fields.Add("message", Value.ForString(_logFormatter.RenderEventMessage(evnt, writer)));

                var propStruct = new Struct();
                foreach (var property in evnt.Properties)
                    _logFormatter.WritePropertyAsJson(log, propStruct, property.Key, property.Value);

                jsonPayload.Fields.Add("properties", Value.ForStruct(propStruct));

                // service name and version are added as extra context data if available
                // these properties are required for any logged exceptions to automatically be picked up by cloud error reporting
                if (_serviceNameAvailable)
                {
                    var contextStruct = new Struct();
                    contextStruct.Fields.Add("service", Value.ForString(_sinkOptions.ServiceName));
                    contextStruct.Fields.Add("version", Value.ForString(_sinkOptions.ServiceVersion));
                    jsonPayload.Fields.Add("serviceContext", Value.ForStruct(contextStruct));
                }

                log.JsonPayload = jsonPayload;
            }
            else
            {
                // text output is simple stringification
                log.TextPayload = _logFormatter.RenderEventMessage(evnt, writer);

                foreach (var property in evnt.Properties)
                    _logFormatter.WritePropertyAsLabel(log, property.Key, property.Value);
            }

            return log;
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

        public Task OnEmptyBatchAsync()
        {
            return Task.CompletedTask;
        }
    }
}
