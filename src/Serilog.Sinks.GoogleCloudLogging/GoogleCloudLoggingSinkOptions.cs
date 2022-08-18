using System.Collections.Generic;

namespace Serilog.Sinks.GoogleCloudLogging;

public class GoogleCloudLoggingSinkOptions
{
    /// <summary>
    /// ID (not name) of Google Cloud Platform project where logs will be sent.
    /// Optional if running in GCP. Required if running elsewhere or to override the destination.
    /// </summary>
    public string? ProjectId { get; set; }

    /// <summary>
    /// Resource type for logs, one of (https://cloud.google.com/logging/docs/api/v2/resource-list).
    /// Optional, will be automatically identified if running in GCP or will be set to "global".
    /// </summary>
    public string? ResourceType { get; set; }

    /// <summary>
    /// Name of individual log.
    /// Optional, will use `SourceContext` from Serilog context if available (see other setting) or will be set to "Default".
    /// </summary>
    public string LogName { get; set; } = "Default";

    /// <summary>
    /// Optional labels added to all log entries.
    /// </summary>
    public Dictionary<string, string> Labels { get; } = new();

    /// <summary>
    /// Optional labels for the resource type added to all log entries.
    /// </summary>
    public Dictionary<string, string> ResourceLabels { get; } = new();

    /// <summary>
    /// If a log entry includes a `SourceContext` property (usually created from a log created with a context) then it will be used as the name of the log.
    /// Enabled by default. Disable to always use the `LogName` as set.
    /// </summary>
    public bool UseSourceContextAsLogName { get; set; }

    /// <summary>
    /// Integrate logs with Cloud Trace by setting `Trace`, `SpanId`, `TraceSampled` properties on the LogEvent.
    /// Enabled by default. Required for Google Cloud Trace Log Correlation.
    /// See https://cloud.google.com/trace/docs/trace-log-integration
    /// </summary>
    public bool UseLogCorrelation { get; set; }

    /// <summary>
    /// Content of Google Cloud JSON credentials file to override using Application Default credentials.
    /// </summary>
    public string? GoogleCredentialJson { get; set; }

    /// <summary>
    /// Attach service name to log entries (added as `serviceContext.service` metadata in `jsonPayload`).
    /// Required for logged exceptions to be forwarded to StackDriver Error Reporting.
    /// </summary>
    /// <remarks>
    /// <c>System.Reflection.Assembly.GetExecutingAssembly().GetName().Name</c> is a good value to use.
    /// </remarks>
    public string? ServiceName { get; set; }

    /// <summary>
    /// Attach service version to log entries (added as `serviceContext.version` metadata in `jsonPayload`).
    /// Required for logged exceptions to be forwarded to StackDriver Error Reporting.
    /// </summary>
    /// <remarks>
    /// <c>System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()</c> is a good value to use.
    /// </remarks>
    public string? ServiceVersion { get; set; }

    /// <summary>
    /// Options for Google Cloud Logging
    /// </summary>
    /// <param name="projectId">
    /// ID (not name) of Google Cloud Platform project where logs will be sent.
    /// Optional if running in GCP. Required if running elsewhere or to override the destination.
    /// </param>
    /// <param name="resourceType">
    /// Resource type for logs, one of (https://cloud.google.com/logging/docs/api/v2/resource-list).
    /// Optional, will be automatically identified if running in GCP or will be set to "global".       
    /// </param>
    /// <param name="logName">
    /// Name of individual log.
    /// Optional, will use `SourceContext` from Serilog context if available (see other setting) or will be set to "Default".
    /// </param>
    /// <param name="labels">Optional labels added to all log entries.</param>
    /// <param name="resourceLabels">Optional labels for the resource type added to all log entries.</param>
    /// <param name="useSourceContextAsLogName">
    /// If a log entry includes a `SourceContext` property (usually created from a log created with a context) then it will be used as the name of the log.
    /// Enabled by default. Disable to always use the `LogName` as set.
    /// </param>
    /// <param name="useLogCorrelation">
    /// Integrate logs with Cloud Trace by setting LogEntry.Trace and LogEntry.SpanId if the LogEvent contains TraceId and SpanId properties.
    /// Enabled by default. Required for Google Cloud Trace Log Correlation.
    /// See https://cloud.google.com/trace/docs/trace-log-integration
    /// </param>
    /// <param name="googleCredentialJson">
    /// Content of Google Cloud JSON credentials file to override using Application Default credentials.
    /// </param>
    /// <param name="serviceName">
    /// Attach service name to log entries (added as `serviceContext.service` metadata in `jsonPayload`).
    /// Required for logged exceptions to be forwarded to StackDriver Error Reporting.
    /// </param>
    /// <param name="serviceVersion">
    /// Attach service version to log entries (added as `serviceContext.version` metadata in `jsonPayload`).
    /// Required for logged exceptions to be forwarded to StackDriver Error Reporting.
    /// </param>
    public GoogleCloudLoggingSinkOptions(
        string? projectId = null,
        string? resourceType = null,
        string? logName = null,
        Dictionary<string, string>? labels = null,
        Dictionary<string, string>? resourceLabels = null,
        bool useSourceContextAsLogName = true,
        bool useLogCorrelation = true,
        string? googleCredentialJson = null,
        string? serviceName = null,
        string? serviceVersion = null)
    {
        ProjectId = projectId;
        ResourceType = resourceType;
        LogName = logName ?? LogName;

        if (labels != null)
            foreach (var kvp in labels)
                Labels[kvp.Key] = kvp.Value;

        if (resourceLabels != null)
            foreach (var kvp in resourceLabels)
                ResourceLabels[kvp.Key] = kvp.Value;

        UseSourceContextAsLogName = useSourceContextAsLogName;
        UseLogCorrelation = useLogCorrelation;
        GoogleCredentialJson = googleCredentialJson;
        ServiceName = serviceName;
        ServiceVersion = serviceVersion;
    }
}