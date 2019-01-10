# Serilog.Sinks.GoogleCloudLogging

Serilog sink that writes events to [Google Cloud Platform Stackdriver Logging](https://cloud.google.com/logging/).

## Getting started

#### Install [package](https://www.nuget.org/packages/Serilog.Sinks.GoogleCloudLogging/) from Nuget:

```
dotnet add package Serilog.Sinks.GoogleCloudLogging
```

#### Configure Logger (using code):

```csharp
var config = new GoogleCloudLoggingSinkOptions("YOUR_PROJECT_ID");
var log = new LoggerConfiguration().WriteTo.GoogleCloudLogging(config).CreateLogger();
```

#### Configure Logger (using config file):

This assumes that you are using ['serilog-settings-configuration'](https://github.com/serilog/serilog-settings-configuration) to allow you to load your sinks in via an `appsettings.json` file. This *only* supports `projectId` and `useJsonOutput` settings.

```json
"Serilog": {
    "Using": [ "Serilog.Sinks.GoogleCloudLogging" ],
    "MinimumLevel": "Warning",
    "WriteTo": [
      { "Name":"GoogleCloudLogging", 
        "Args":
        {
          "projectID": "YOUR_PROJECT_ID",
          "useJsonOutput": "true"
        }
      }
    ]
  }
```

#### GCP authentication:

This library uses the [`Google-Cloud-Dotnet`](https://googlecloudplatform.github.io/google-cloud-dotnet/) libraries which authenticate using the default service account on the machine. This is automatic on GCE VMs or you can use the [`gcloud`](https://cloud.google.com/sdk/) SDK to authenticate manually. The service account must have the [`Logs Writer`](https://cloud.google.com/logging/docs/access-control) permission to send logs.

## Sink Options

Name | Required | Default | Description
---- | -------- | ------- | -----------
`ProjectId` | Yes | | Google Cloud project ID where logs will be sent to. 
`ResourceType` | Yes | `global` | Resource type for all log output. Must be one of the supported types listed in the  [cloud logging documentation](https://cloud.google.com/logging/docs/api/v2/resource-list).
`LogName` | Yes | `Default` | Name of log under the resource type.
`Labels` | | | Dictionary<string, string> of properties added to all log entries.
`ResourceLabels` | | | Dictionary<string, string> of properties added to all log entries, at the resource level.
`UseSourceContextAsLogName` | | True | The log name for a log entry will be set to the [SourceContext](https://github.com/serilog/serilog/wiki/Writing-Log-Events#source-contexts) property if it's available.
`UseJsonOutput` | | False | Structured logging can be sent as text with labels or as a JSON object, details below.

#### Output Type

Serilog uses structured logging, which means each log statement has a formatting template with attached properties that are combined to create the final output. When `UseJsonOutput` is false, the final output is sent as the `TextPayload` to GCP logs with any properties serialized as string key/value labels.

If you want to maintain data types, set `UseJsonOutput` to true and the output will be sent as the `JsonPayload` with log structure intact as much as possible. This is slightly slower but helpful for querying child properties or numeric values in the Log Viewer, and will also capture property names even if they have null values.

## Viewing Logs

Logs will appear in the Google Cloud Console Log Viewer: https://console.cloud.google.com/logs/viewer

When using default options, logs will appear under these filter settings:



![](https://i.imgur.com/3lk1LLM.png)
