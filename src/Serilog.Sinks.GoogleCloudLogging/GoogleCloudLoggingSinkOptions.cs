using System.Collections.Generic;

namespace Serilog.Sinks.GoogleCloudLogging
{
    public class GoogleCloudLoggingSinkOptions
    {
        public string ProjectId { get; }
        public string ResourceType { get; } = "global";
        public string LogName { get; } = "Default";
        public Dictionary<string, string> Labels { get; } = new Dictionary<string, string>();

        /// <summary>
        /// If a log entry includes a `SourceContext` property (usually created from a log created with a context) then it will be used as the name of the log.
        /// Defaults to true. Disable to always use custom log name setup in options.
        /// </summary>
        public bool UseSourceContextAsLogName { get; set; } = true;

        /// <summary>
        /// Options for Google Cloud Logging
        /// </summary>
        /// <param name="projectId">ID of project where logs will be sent.</param>
        /// <param name="resourceType">Resource type for logs. Default is "global" which shows as "Global" in Google Cloud Console UI.</param>
        /// <param name="logName">Name of individual log, will use SourceContext property automatically from Serilog context if it's available or fallback to this setting. Default is "Default".</param>
        /// <param name="labels">Labels added to every log entry in addition to any properties for each statement.</param>
        public GoogleCloudLoggingSinkOptions(string projectId, string resourceType = null, string logName = null, Dictionary<string, string> labels = null)
        {
            ProjectId = projectId;

            if (resourceType != null)
                ResourceType = resourceType;

            if (logName != null)
                LogName = logName;

            if (labels != null)
                Labels = labels;
        }
    }
}
