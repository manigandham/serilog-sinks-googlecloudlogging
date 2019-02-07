using System.Collections.Generic;

namespace Serilog.Sinks.GoogleCloudLogging
{
    public class GoogleCloudLoggingSinkOptions
    {
        public string ProjectId { get; }
        public string ResourceType { get; } = "global";
        public string LogName { get; } = "Default";
        public string GoogleCredentialJson { get; } 

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
        public bool UseSourceContextAsLogName { get; set; } = true;

        /// <summary>
        /// Logs are normally sent as a formatted line of text with attached properties serialized as a flat list of string labels.
        /// This option allows for serializing the log and properties as a JSON object instead, to maintain data types and structure as much as possible for richer querying within GCP Log Viewer.
        /// Defaults to false.
        /// </summary>
        public bool UseJsonOutput { get; set; } = false;

        /// <summary>
        /// Options for Google Cloud Logging
        /// </summary>
        /// <param name="projectId">ID of project where logs will be sent.</param>
        /// <param name="resourceType">Resource type for logs. Default is "global" which shows as "Global" in Google Cloud Console UI.</param>
        /// <param name="logName">Name of individual log, will use SourceContext property automatically from Serilog context if it's available or fallback to this setting. Default is "Default".</param>
        /// <param name="labels">Additional custom labels added to all log entries.</param>
        /// <param name="resourceLabels">Additional custom labels for the resource type added to all log entries.</param>
        public GoogleCloudLoggingSinkOptions(string projectId, string resourceType = null, string logName = null, Dictionary<string, string> labels = null, Dictionary<string, string> resourceLabels = null, string googleCredentialJson = null)
        {
            ProjectId = projectId;
            GoogleCredentialJson = googleCredentialJson;

            if (resourceType != null)
                ResourceType = resourceType;

            if (logName != null)
                LogName = logName;

            if (labels != null)
            {
                foreach (var kvp in labels)
                    Labels[kvp.Key] = kvp.Value;
            }

            if (resourceLabels != null)
            {
                foreach (var kvp in resourceLabels)
                    ResourceLabels[kvp.Key] = kvp.Value;
            }
        }
    }
}
