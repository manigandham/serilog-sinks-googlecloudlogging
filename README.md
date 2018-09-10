# Serilog.Sinks.GoogleCloudLogging

Serilog sink that writes events to [Google Cloud Platform Stackdriver Logging](https://cloud.google.com/logging/).

### Getting started

Install [package](https://www.nuget.org/packages/Serilog.Sinks.GoogleCloudLogging/) from Nuget:

```
Install-Package Serilog.Sinks.GoogleCloudLogging
```

Configure Logger:

```csharp
var log = new LoggerConfiguration()
    .WriteTo.GoogleCloudLogging(new GoogleCloudLoggingSinkOptions("YOUR_PROJECT_ID"))
    .CreateLogger();
```

This library uses the [`Google-Cloud-Dotnet`](https://googlecloudplatform.github.io/google-cloud-dotnet/) libraries which authenticate using the default service account on the machine. This is automatic on GCE VMs or you can use the [`gcloud`](https://cloud.google.com/sdk/) SDK to authenticate manually. The service account must have the [`Logs Writer`](https://cloud.google.com/logging/docs/access-control) permission to send logs.

### Sink Options

Name | Required | Default | Description
---- | -------- | ------- | -----------
`ProjectId` | Yes | | Google Cloud project ID where logs will be sent to. 
`ResourceType` | Yes | global | Resource type for all log output. Must be one of the supported types listed in the  [cloud logging documentation](https://cloud.google.com/logging/docs/api/v2/resource-list).
`LogName` | Yes | Default | Name of log under the resource type.
`Labels` | | | Dictionary<string, string> of properties added to all log entries.
`ResourceLabels` | | | Dictionary<string, string> of properties added to all log entries, at the resource level.
`UseSourceContextAsLogName` | | True | The log name for a log entry will be set to the [SourceContext](https://github.com/serilog/serilog/wiki/Writing-Log-Events#source-contexts) property if it's available.
`UseJsonOutput` | | False | Structured logging can be sent as text with labels or as a JSON object, details below.

### Output Type

Serilog uses structured logging so each log statement has a formatting template with attached properties which are then combined to create the final output. When `UseJsonOutput` is false, the final output is rendered as the `TextPayload` in GCP logs with any properties serialized as string key/value labels.

To maintain the datatypes and structure as much as possible, set `UseJsonOutput` to true and the log statement will be serialized and sent as the `JsonPayload` instead. This is slightly slower but helpful for querying child properties or numbers in the Log Viewer, and will also capture property names when they have null values.

### Viewing Logs

Logs will appear in the Google Cloud Console Log Viewer: https://console.cloud.google.com/logs/viewer

When using default options, logs will appear under these filter settings:

![](https://i.imgur.com/azT3uDE.png)
