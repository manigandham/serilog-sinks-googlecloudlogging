using System.Collections.Generic;

namespace Serilog.Sinks.GoogleCloudLogging
{
    public class GoogleCloudLoggingSinkOptions
    {
        /// <summary>
        /// ID (not name) of Google Cloud Platform project where logs will be sent.
        /// If not set, will be automatically sent to the project ID hosting the program if running in GCP.
        /// Required if running elsewhere or to override the destination.
        /// </summary>
        public string ProjectId { get; set; }

        /// <summary>
        /// Resource type for logs, one of (https://cloud.google.com/logging/docs/api/v2/resource-list).
        /// If value not provided then if running in GCP it will be automatically identified, otherwise default is "global" which shows as "Global" in Google Cloud Console UI.
        /// </summary>
        public string ResourceType { get; set; }

        /// <summary>
        /// Name of individual log.
        /// Will be set to `SourceContext` property automatically from Serilog context if available, or fallback to this setting. Default is "Default".
        /// </summary>
        public string LogName { get; set; }

        /// <summary>
        /// Additional custom labels added to all log entries.
        /// </summary>
        public Dictionary<string, string> Labels { get; } = new Dictionary<string, string>();

        /// <summary>
        /// Additional custom labels for the resource type added to all log entries.
        /// </summary>
        public Dictionary<string, string> ResourceLabels { get; } = new Dictionary<string, string>();

        /// <summary>
        /// If a log entry includes a `SourceContext` property (usually created from a log created with a context) then it will be used as the name of the log.
        /// Defaults to true. Disable to always use the `LogName` as set in options.
        /// </summary>
        public bool UseSourceContextAsLogName { get; set; }

        /// <summary>
        /// Logs are normally sent as a formatted line of text with attached properties serialized as strings.
        /// Enabling this option serializes the log and properties as a JSON object instead, which maintains data types and structure as much as possible for richer querying within GCP Log Viewer.
        /// Defaults to false. This must be set to True for logged exceptions to be forwarded to StackDriver Error Reporting.
        /// </summary>
        public bool UseJsonOutput { get; set; }

        /// <summary>
        /// Integrate logs with Cloud Trace by setting LogEntry.Trace and LogEntry.SpanId if the LogEvent contains TraceId and SpanId properties.
        /// Required for Google Cloud Trace Log Correlation.
        /// See https://cloud.google.com/trace/docs/trace-log-integration
        /// </summary>
        public bool UseLogCorrelation { get; set; }

        /// <summary>
        /// JSON string of Google Cloud credentials file, otherwise will use Application Default credentials found on host by default.
        /// </summary>
        public string GoogleCredentialJson { get; set; }

        /// <summary>
        /// Added as "serviceContext.service" metadata in "jsonPayload". This is required for logged exceptions to be forwarded to StackDriver Error Reporting and "UseJsonOutput" must be True.
        /// System.Reflection.Assembly.GetExecutingAssembly().GetName().Name is a good value to use.
        /// </summary>
        public string ServiceName { get; set; }

        /// <summary>
        /// Added as "serviceContext.version" metadata in "jsonPayload". This is required for logged exceptions to be forwarded to StackDriver Error Reporting and "UseJsonOutput" must be True.
        /// System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() is a good value to use.
        /// </summary>
        public string ServiceVersion { get; set; }

        /// <summary>
        /// Options for Google Cloud Logging
        /// </summary>
        /// <param name="projectId">ID (not name) of Google Cloud project where logs will be sent.</param>
        /// <param name="resourceType">Resource type for logs. Default is "global" which shows as "Global" in Google Cloud Console UI.</param>
        /// <param name="logName">Name of individual log. Will be set to `SourceContext` property automatically from Serilog context if it's available or fallback to this setting. Default is "Default".</param>
        /// <param name="labels">Additional custom labels added to all log entries.</param>
        /// <param name="resourceLabels">Additional custom labels for the resource type added to all log entries.</param>
        /// <param name="useSourceContextAsLogName"></param>
        /// <param name="useJsonOutput"></param>
        /// <param name="useLogCorrelation"></param>
        /// <param name="googleCredentialJson">JSON string of Google Cloud credentials file, otherwise will use Application Default credentials found on host by default.</param>
        /// <param name="serviceName">Name of service, added as "serviceContext.service" metadata.</param>
        /// <param name="serviceVersion">Version of service, added as "serviceContext.version" metadata.</param>
        public GoogleCloudLoggingSinkOptions(
            string projectId = null,
            string resourceType = null,
            string logName = null,
            Dictionary<string, string> labels = null,
            Dictionary<string, string> resourceLabels = null,
            bool useSourceContextAsLogName = true,
            bool useJsonOutput = false,
            bool useLogCorrelation = true,
            string googleCredentialJson = null,
            string serviceName = null,
            string serviceVersion = null)
        {
            ProjectId = projectId;
            ResourceType = resourceType;
            LogName = logName ?? "Default";

            if (labels != null)
                foreach (var kvp in labels)
                    Labels[kvp.Key] = kvp.Value;

            if (resourceLabels != null)
                foreach (var kvp in resourceLabels)
                    ResourceLabels[kvp.Key] = kvp.Value;

            UseSourceContextAsLogName = useSourceContextAsLogName;
            UseJsonOutput = useJsonOutput;
            UseLogCorrelation = useLogCorrelation;
            GoogleCredentialJson = googleCredentialJson;
            ServiceName = serviceName;
            ServiceVersion = serviceVersion ?? "<Unknown>";
        }
    }
}
