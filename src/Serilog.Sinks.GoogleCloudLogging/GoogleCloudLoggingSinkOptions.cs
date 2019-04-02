using System.Collections.Generic;

namespace Serilog.Sinks.GoogleCloudLogging
{
    public class GoogleCloudLoggingSinkOptions
    {
        /// <summary>
        /// ID (not name) of Google Cloud project where logs will be sent. 
        /// </summary>
        public string ProjectId { get; }

        /// <summary>
        /// Resource type for logs. Default is "global" which shows as "Global" in Google Cloud Console UI.
        /// </summary>
        public string ResourceType { get; set; }

        /// <summary>
        /// Name of individual log. Will be set to `SourceContext` property automatically from Serilog context if it's available or fallback to this setting. Default is "Default".
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
        /// Logs are normally sent as a formatted line of text with attached properties serialized as a flat list of string labels.
        /// This option allows for serializing the log and properties as a JSON object instead, to maintain data types and structure as much as possible for richer querying within GCP Log Viewer.
        /// This must be set to True for errors to be forwarded to ErrorReporting.
        /// Defaults to false.
        /// </summary>
        public bool UseJsonOutput { get; set; }

        /// <summary>
        /// JSON string of Google Cloud credentials file, otherwise will use Application Default credentials found on host by default.
        /// </summary>
        public string GoogleCredentialJson { get; set; }

        /// <summary>
        /// If log entry is severity Error this will be the "service" in the "serviceContext" JSON node. This is required for errors to be forwarded to StackDriver ErrorReporting and "UseJsonOutput" must be True.
        /// System.Reflection.Assembly.GetExecutingAssembly().GetName().Name is a good value to use
        /// </summary>
        public string ErrorReportingServiceName { get; set; }

        /// <summary>
        /// If log entry is severity Error this will be the "version" in the "serviceContext" JSON node. This is required for errors to be forwarded to StackDriver ErrorReporting and "UseJsonOutput" must be True.
        /// System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() is a good value to use
        /// </summary>
        public string ErrorReportingServiceVersion { get; set; }

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
        /// <param name="googleCredentialJson">JSON string of Google Cloud credentials file, otherwise will use Application Default credentials found on host by default.</param>
        /// <param name="errorReportingServiceName">Valid only for error severity log entries, this is "serviceContext.service" in the jsonPayload.</param>
        /// <param name="errorReportingServiceVersion">Valid only for error severity log entries, this is "serviceContext.version" in the jsonPayload.</param>
        public GoogleCloudLoggingSinkOptions(
            string projectId,
            string resourceType = null,
            string logName = null,
            Dictionary<string, string> labels = null,
            Dictionary<string, string> resourceLabels = null,
            bool useSourceContextAsLogName = true,
            bool useJsonOutput = false,
            string googleCredentialJson = null,
            string errorReportingServiceName = null,
            string errorReportingServiceVersion = null
        )
        {
            ProjectId = projectId;
            ResourceType = resourceType ?? "global";
            LogName = logName ?? "Default";

            if (labels != null)
                foreach (var kvp in labels)
                    Labels[kvp.Key] = kvp.Value;

            if (resourceLabels != null)
                foreach (var kvp in resourceLabels)
                    ResourceLabels[kvp.Key] = kvp.Value;

            UseSourceContextAsLogName = useSourceContextAsLogName;
            UseJsonOutput = useJsonOutput;
            GoogleCredentialJson = googleCredentialJson;
            ErrorReportingServiceName = errorReportingServiceName;
            ErrorReportingServiceVersion = errorReportingServiceVersion ?? "<Unknown>";
        }
    }
}
