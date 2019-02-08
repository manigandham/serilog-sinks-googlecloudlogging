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

This assumes that you are using ['serilog-settings-configuration'](https://github.com/serilog/serilog-settings-configuration) to allow you to load your sinks in via an `appsettings.json` file. This *only* supports `projectId`, `useJsonOutput` and `resourceType` / `resourceLabels` settings.

```json
"Serilog": {
    "Using": [ "Serilog.Sinks.GoogleCloudLogging" ],
    "MinimumLevel": "Warning",
    "WriteTo": [
      { "Name":"GoogleCloudLogging", 
        "Args":
        {
          "projectID": "YOUR_PROJECT_ID",
          "useJsonOutput": "true",
          "resourceType": "k8s_cluster",
          "resourceLabels": {
            "project_id": "PROJECT-ID-HERE-12345",
            "location": "LOCATION-STRING-HERE-region-name",
            "cluster_name": "CLUSTER-NAME-HERE-container-cluster"
          }
        }
      }
    ]
  }
```

See [Monitored Resources and Services](https://cloud.google.com/logging/docs/api/v2/resource-list) for the correct `resourceLabels`.

#### GCP authentication:

This library uses the [`Google-Cloud-Dotnet`](https://googlecloudplatform.github.io/google-cloud-dotnet/) libraries which authenticate using the default service account on the machine. This is automatic on GCE VMs or you can use the [`gcloud`](https://cloud.google.com/sdk/) SDK to authenticate manually. The service account must have the [`Logs Writer`](https://cloud.google.com/logging/docs/access-control) permission to send logs.

## Sink Options

Name | Required | Default | Description
---- | -------- | ------- | -----------
`ProjectId` | Yes | | Google Cloud project ID where logs will be sent. 
`ResourceType` | Yes | `global` | Resource type for all log output. Must be one of the supported types listed in the  [cloud logging documentation](https://cloud.google.com/logging/docs/api/v2/resource-list).
`LogName` | Yes | `Default` | Name of the log.
`Labels` | | | `Dictionary<string, string>` of properties added to all log entries.
`ResourceLabels` | | | `Dictionary<string, string>` of properties added to all log entries, at the resource level.
`UseSourceContextAsLogName` | | True | The log name for a log entry will be set to the [SourceContext](https://github.com/serilog/serilog/wiki/Writing-Log-Events#source-contexts) property if it's available.
`UseJsonOutput` | | False | Structured logs can be sent as text with labels or as a JSON object, see details below.

#### Output Type

Serilog uses structured logging which means each log line is a formatting template with attached properties that are combined to create the final output. When `UseJsonOutput` is false, the output is sent as `TextPayload` to GCP with any properties serialized to string key/value labels.

If `UseJsonOutput` is set to true, the output will be sent as `JsonPayload` to maintain the original data types. This is helpful for querying child properties or numeric values in the Log Viewer, and will also capture property names even if they have null values. 

WARNING: JSON output only accepts numeric values as `double` so all numbers will be converted. Large integers and floating-point values will lose precision. If you want the exact value preserved then send then log it as a string instead.

## Viewing Logs

Logs will appear in the Google Cloud Console Log Viewer: https://console.cloud.google.com/logs/viewer

When using default options, logs will appear under these filter settings:



![](https://i.imgur.com/3lk1LLM.png)
