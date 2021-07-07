using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Google.Api;
using Google.Api.Gax.Grpc;
using Google.Cloud.Logging.Type;
using Google.Cloud.Logging.V2;
using Google.Protobuf.WellKnownTypes;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Sinks.PeriodicBatching;

namespace Serilog.Sinks.GoogleCloudLogging
{
    public class GoogleCloudLoggingSink : IBatchedLogEventSink
    {
        private readonly GoogleCloudLoggingSinkOptions _sinkOptions;
        private readonly LoggingServiceV2Client _client;
        private readonly MonitoredResource _resource;
        private readonly string _projectId;
        private readonly string _logName;
        private readonly LogFormatter _logFormatter;
        private readonly Struct? _serviceContext;

        public GoogleCloudLoggingSink(GoogleCloudLoggingSinkOptions sinkOptions, ITextFormatter? textFormatter)
        {
            _sinkOptions = sinkOptions;

            // retrieve current environment details automatically for GCE, GKE, GAE, or Cloud Run
            // set any user-provided resource type and labels which will override existing values from environment
            _resource = MonitoredResourceBuilder.FromPlatform();
            _resource.Type = _sinkOptions.ResourceType ?? _resource.Type;
            foreach (var kvp in _sinkOptions.ResourceLabels)
                _resource.Labels[kvp.Key] = kvp.Value;

            _projectId = _sinkOptions.ProjectId ?? (_resource.Labels.TryGetValue("project_id", out string id) ? id : null) ?? "";
            if (String.IsNullOrWhiteSpace(_projectId))
                throw new ArgumentNullException(nameof(_projectId), "Project Id is not provided and could not be automatically discovered.");

            if (String.IsNullOrWhiteSpace(_sinkOptions.LogName))
                throw new ArgumentNullException(nameof(_sinkOptions.LogName), "Log Name is blank. Check assigned value or unset to use default.");

            _logName = LogFormatter.CreateLogName(_projectId, _sinkOptions.LogName);
            _logFormatter = new LogFormatter(textFormatter);

            // cache struct for service name and version contextual properties if available
            // these properties are required for any logged exceptions to automatically be picked up by cloud error reporting
            if (!String.IsNullOrWhiteSpace(_sinkOptions.ServiceName))
            {
                _serviceContext = new Struct();
                _serviceContext.Fields.Add("service", Value.ForString(_sinkOptions.ServiceName));
                if (!String.IsNullOrWhiteSpace(_sinkOptions.ServiceVersion))
                    _serviceContext.Fields.Add("version", Value.ForString(_sinkOptions.ServiceVersion));
            }

            // logging client for google cloud apis
            _client = _sinkOptions.GoogleCredentialJson != null
                ? new LoggingServiceV2ClientBuilder { JsonCredentials = _sinkOptions.GoogleCredentialJson }.Build()
                : LoggingServiceV2Client.Create();
        }

        public Task EmitBatchAsync(IEnumerable<LogEvent> events)
        {
            using var writer = new StringWriter();
            var entries = new List<LogEntry>();
            foreach (var evnt in events)
                entries.Add(CreateLogEntry(evnt, writer));

            return entries.Count > 0
                ? _client.WriteLogEntriesAsync(_logName, _resource, _sinkOptions.Labels, entries, CancellationToken.None)
                : Task.CompletedTask;
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
                {
                    if (!TryWriteSpecialProperty(log, property.Key, property.Value))
                    {
                        _logFormatter.WritePropertyAsJson(log, propStruct, property.Key, property.Value);
                    }
                }

                jsonPayload.Fields.Add("properties", Value.ForStruct(propStruct));

                if (_serviceContext != null)
                    jsonPayload.Fields.Add("serviceContext", Value.ForStruct(_serviceContext));

                log.JsonPayload = jsonPayload;
            }
            else
            {
                // text output is simple stringification
                log.TextPayload = _logFormatter.RenderEventMessage(evnt, writer);

                foreach (var property in evnt.Properties)
                {
                    if (!TryWriteSpecialProperty(log, property.Key, property.Value))
                    {
                        _logFormatter.WritePropertyAsLabel(log, property.Key, property.Value);
                    }
                }
            }

            return log;
        }

        private bool TryWriteSpecialProperty(LogEntry log, string key, LogEventPropertyValue value)
        {
            if (_sinkOptions.UseSourceContextAsLogName && key.Equals("SourceContext", StringComparison.OrdinalIgnoreCase))
            {
                log.LogName = LogFormatter.CreateLogName(_projectId, GetString(value));
                return true;
            }

            if (_sinkOptions.UseLogCorrelation && key.Equals("TraceId", StringComparison.OrdinalIgnoreCase))
            {
                log.Trace = $"projects/{_projectId}/traces/{GetString(value)}";
                return true;
            }

            if (_sinkOptions.UseLogCorrelation && key.Equals("SpanId", StringComparison.OrdinalIgnoreCase))
            {
                log.SpanId = GetString(value);
                return true;
            }

            if (_sinkOptions.UseLogCorrelation && key.Equals("TraceSampled", StringComparison.OrdinalIgnoreCase))
            {
                log.TraceSampled = GetBoolean(value);
                return true;
            }

            return false;

            static string GetString(LogEventPropertyValue v) => (v as ScalarValue)?.Value?.ToString() ?? "";
            static bool GetBoolean(LogEventPropertyValue v) => (v as ScalarValue)?.Value is true;
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

        public Task OnEmptyBatchAsync() => Task.CompletedTask;
    }
}
