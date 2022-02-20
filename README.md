# Serilog.Sinks.GoogleCloudLogging

Serilog sink that writes events to [Google Cloud Logging](https://cloud.google.com/logging/).

Built for `net6.0`, `net5.0`, `netstandard2.0`

Release notes here: [CHANGELOG.md](CHANGELOG.md)

## Usage

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

This requires the [`serilog-settings-configuration`](https://github.com/serilog/serilog-settings-configuration) package.

```json
// appsettings.json or other config file
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

## GCP Authentication:

This library uses the [Google Cloud .NET](https://cloud.google.com/dotnet/docs) client libraries which authenticate using the [Application Default Credentials](https://cloud.google.com/docs/authentication/production#providing_credentials_to_your_application) found on the host. This is automatic on Google Cloud (GCE, GKE, Cloud Run, AppEngine) or you can use the [gcloud SDK](https://cloud.google.com/sdk/) to authenticate manually. 

The service account requires the [`Logs Writer`](https://cloud.google.com/logging/docs/access-control) permission to send logs.

## Sink Options

Name | Default | Description
---- | ------- | -----------
`ProjectId` | | Google Cloud project ID where logs will be sent. Will be automatically set to host project if running in GCP, otherwise required.
`ResourceType` | `"global"` | Resource type for all log output. Will be automatically discovered if running in GCP, otherwise required. See [Monitored Resources and Services](https://cloud.google.com/logging/docs/api/v2/resource-list) for supported types.
`LogName` | `"Default"` | Name of the log.
`Labels` | | `Dictionary<string, string>` of properties added to all log entries.
`ResourceLabels` | | `Dictionary<string, string>` of properties added to all log entries, at the resource level. See [Monitored Resources and Services](https://cloud.google.com/logging/docs/api/v2/resource-list) for recognized labels.
`UseSourceContextAsLogName` | True | The log name for a log entry will be set to the [SourceContext](https://github.com/serilog/serilog/wiki/Writing-Log-Events#source-contexts) property if available.
`UseJsonOutput` | False | Serialize log entries as JSON for structured logging. See details below.
`UseLogCorrelation` | False | Integrate logs with [Cloud Trace](https://cloud.google.com/trace) by setting `Trace`, `SpanId`, `TraceSampled` properties if available.
`ServiceName` | | Name of the service added as metadata to log entries. Required to forward logged exceptions to Google Cloud Error Reporting. Must also set `UseJsonOutput` to true.
`ServiceVersion` | | Version of the service added as metadata to log entries.
`GoogleCredentialJson` | | GCP client libraries use [Application Default Credentials](https://cloud.google.com/docs/authentication/production#providing_credentials_to_your_application). If these are not available or you need to use other credentials, set the content of a JSON credential file directly.

## Logging Output

Serilog uses structured logging but logs are sent to GCP as a `TextPayload` with properties serialized to string labels by default. Enable `UseJsonOutput` to send logs as a `JsonPayload` with the proper data types. This provides richer logs with better querying support and will also capture property names even if they have null values.

*NOTE*: JSON output only accepts numeric values as `double` so all numbers will be converted. Large integers and floating-point values will lose precision. If you want the exact value preserved then log it as a string instead.

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

#### Viewing Logs

View and query logs in the Google Cloud Console Logs Explorer: https://console.cloud.google.com/logs/viewer
