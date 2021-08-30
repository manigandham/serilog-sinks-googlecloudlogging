# Serilog.Sinks.GoogleCloudLogging

Serilog sink that writes events to [Google Cloud Logging](https://cloud.google.com/logging/).

Built for `netstandard2.0`, `net5.0`

Release notes here: [CHANGELOG.md](CHANGELOG.md)

## Getting started

#### Install [package from Nuget](https://www.nuget.org/packages/Serilog.Sinks.GoogleCloudLogging/):

```
dotnet add package Serilog.Sinks.GoogleCloudLogging
```

#### Configure Logger (using code):

```csharp
var config = new GoogleCloudLoggingSinkOptions { ProjectId = "YOUR_PROJECT_ID", UseJsonOutput = true };
Log.Logger = new LoggerConfiguration().WriteTo.GoogleCloudLogging(config).CreateLogger();
```

#### Configure Logger (using config file):

This requires ['serilog-settings-configuration'](https://github.com/serilog/serilog-settings-configuration) to load sinks using an `appsettings.json` file.

```json
"Serilog": {
  "Using": [ "Serilog.Sinks.GoogleCloudLogging" ],
  "MinimumLevel": "Information",
  "WriteTo": [
    {
      "Name": "GoogleCloudLogging",
      "Args": {
        "projectID": "YOUR_PROJECT_ID",
        "useJsonOutput": "true",
        "resourceType": "k8s_cluster",
        "resourceLabels": {
          "project_id": "PROJECT-ID-HERE-12345",
          "location": "LOCATION-STRING-HERE-region-name",
          "cluster_name": "CLUSTER-NAME-HERE-container-cluster"
        },
        "restrictedToMinimumLevel": "Warning"
      }
    }
  ]
}
```
```csharp
var config = new ConfigurationBuilder().AddJsonFile("appsettings.json", optional: true, reloadOnChange: true).Build();
Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(config).CreateLogger();
```

#### GCP authentication:

This library uses the [`Google-Cloud-Dotnet`](https://googleapis.github.io/google-cloud-dotnet/) libraries which authenticate using the [Application Default Credentials](https://cloud.google.com/docs/authentication/production#providing_credentials_to_your_application) found on the host. This is automatic on GCE VMs or you can use the [`gcloud`](https://cloud.google.com/sdk/) SDK to authenticate manually. The service account must have the [`Logs Writer`](https://cloud.google.com/logging/docs/access-control) permission to send logs.

## Sink Options

Name | Default | Description
---- | ------- | -----------
`ProjectId` | | Google Cloud project ID where logs will be sent. Will be automatically set to host project if running in GCP, otherwise required.
`ResourceType` | `global` | Resource type for all log output. Will be automatically discovered if running in GCP, otherwise required. Must be one of the supported types listed in the  [cloud logging documentation](https://cloud.google.com/logging/docs/api/v2/resource-list).
`LogName` | `Default` | Name of the log. This is required if `UseSourceContextAsLogName` is false.
`Labels` | | `Dictionary<string, string>` of properties added to all log entries.
`ResourceLabels` | | `Dictionary<string, string>` of properties added to all log entries, at the resource level. See [Monitored Resources and Services](https://cloud.google.com/logging/docs/api/v2/resource-list) for recognized labels.
`UseSourceContextAsLogName` | True | The log name for a log entry will be set to the [SourceContext](https://github.com/serilog/serilog/wiki/Writing-Log-Events#source-contexts) property if it's available.
`UseJsonOutput` | False | Serialize log entries as JSON objects instead of strings to preserve structure and data types for rich querying. See details below.
`UseLogCorrelation` | False | Integrate logs with Cloud Trace by setting `Trace`, `SpanId`, `TraceSampled` properties on the LogEvent.
`GoogleCredentialJson` | | GCP client libraries use [Application Default Credentials](https://cloud.google.com/docs/authentication/production#providing_credentials_to_your_application). If these are not available or you need to use other credentials, set the JSON text of a credential file directly.
`ServiceName` | | Name of the service added as metadata to log entries. Required to forward logged exceptions to Google Cloud Error Reporting. Must also set `UseJsonOutput` to true.
`ServiceVersion` | | Version of the service added as metadata to log entries.

#### Output Type

Serilog uses structured logging which means each log line is a formatting template with attached properties that are combined to create the final output. When `UseJsonOutput` is false, the output is sent as `TextPayload` to GCP with any properties serialized to string key/value labels.

If `UseJsonOutput` is set to true, the output will be sent as `JsonPayload` to maintain the original data types. This is helpful for querying child properties or numeric values in the Log Viewer, and will also capture property names even if they have null values.

WARNING: JSON output only accepts numeric values as `double` so all numbers will be converted. Large integers and floating-point values will lose precision. If you want the exact value preserved then log it as a string instead.

#### Log Level Mapping

This table shows the mapping from Serilog [`LogLevel`](https://github.com/serilog/serilog/wiki/Configuration-Basics#minimum-level) to Google Cloud Logging [`LogSeverity`](https://cloud.google.com/logging/docs/reference/v2/rest/v2/LogEntry#LogSeverity)

Serilog | Cloud Logging
------------- | -----------------
Verbose | Debug
Debug | Debug
Information | Info
Warning | Warning
Error | Error
Fatal | Critical

## Viewing Logs

Logs will appear in the Google Cloud Console Log Viewer: https://console.cloud.google.com/logs/viewer
