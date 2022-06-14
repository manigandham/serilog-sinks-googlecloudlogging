# Serilog.Sinks.GoogleCloudLogging

Serilog sink that writes events to [Google Cloud Logging](https://cloud.google.com/logging/).

-   Built for `net6.0`, `net5.0`, `netstandard2.0`
-   [Release Notes](CHANGELOG.md)

## Usage

#### Install [package from Nuget](https://www.nuget.org/packages/Serilog.Sinks.GoogleCloudLogging/):

```
dotnet add package Serilog.Sinks.GoogleCloudLogging
```

#### Configure in code:

```csharp
var config = new GoogleCloudLoggingSinkOptions { ProjectId = "YOUR_PROJECT_ID" };
Log.Logger = new LoggerConfiguration().WriteTo.GoogleCloudLogging(config).CreateLogger();
```

#### Or configure with config file:

This requires the [`serilog-settings-configuration`](https://github.com/serilog/serilog-settings-configuration) package.

```json
"Serilog": {
  "Using": [ "Serilog.Sinks.GoogleCloudLogging" ],
  "MinimumLevel": "Information",
  "WriteTo": [
    {
      "Name": "GoogleCloudLogging",
      "Args": {
        "projectID": "PROJECT-ID-12345",
        "restrictedToMinimumLevel": "Information",
        "labels": {
          "foo": "bar"
        }
      }
    }
  ]
}
```

```csharp
var config = new ConfigurationBuilder().AddJsonFile("appsettings.json", optional: true, reloadOnChange: true).Build();
Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(config).CreateLogger();
```

-   Serilog example for .NET 6: https://blog.datalust.co/using-serilog-in-net-6/

## GCP Integration

### Authentication

This library uses the [Google Cloud .NET](https://cloud.google.com/dotnet/docs) client and [Application Default Credentials](https://cloud.google.com/docs/authentication/production#providing_credentials_to_your_application). The [`Logs Writer`](https://cloud.google.com/logging/docs/access-control) permission is required to send logs. There are several different ways to set credentials:

-   GCE, GKE, Cloud Run, AppEngine and other managed services will have the Application Default Credentials set to the active service account for the resource and can be used without any additional configuration.
-   Authenticate manually with the [gcloud SDK](https://cloud.google.com/sdk/) on a server to set the Application Default Credentials.
-   Set the `GOOGLE_APPLICATION_CREDENTIALS` environment variable to specify the path to your JSON credentials file.
-   Set the `GoogleCredentialJson` config option to pass in the contents of your JSON credentials file.

### Log Output

-   Serilog is designed for **[structured logging](https://github.com/serilog/serilog/wiki/Structured-Data)** which is fully supported by Google Cloud. Logs are sent as a JSON object (`JsonPayload` in the protobuf API) with labels, properties, metadata and any other data like stack traces automatically attached.
-   **Numeric values in labels and properties will be converted to `double` during serialization** because that is the only numeric type supported by JSON. Large integers and floating-point values will lose precision. If you want the exact value preserved then log it as a string instead.
-   GCP Error Reporting is separate from Cloud Logging and **will automatically capture error messages only if they have an `Exception` attached**. It is _not_ based on log severity level.
-   View logs in the GCP Logs Explorer: https://console.cloud.google.com/logs/viewer

## Sink Options

| Option                    | Description                                                                                                                                                                                                             |
| ------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| ProjectId                 | ID (not name) of GCP project where logs will be sent. Optional if running in GCP. Required if running elsewhere or to override the destination.                                                                         |
| ResourceType              | Resource type for logs. Automatically identified if running in GCP or will default to "global". See [Monitored Resources and Services](https://cloud.google.com/logging/docs/api/v2/resource-list) for supported types. |
| LogName                   | Name of the log. Default is "Default", or will use `SourceContext` is setting is enabled.                                                                                                                               |
| Labels                    | Optional `Dictionary<string, string>` labels added to all log entries.                                                                                                                                                  |
| ResourceLabels            | Optional `Dictionary<string, string>` labels added to all log entries, for the resource type. See [Monitored Resources and Services](https://cloud.google.com/logging/docs/api/v2/resource-list) for recognized labels. |
| ServiceName               | Name of the service added as metadata to log entries. Required for logged exceptions to be forwarded to StackDriver Error Reporting.                                                                                    |
| ServiceVersion            | Version of the service added as metadata to log entries. Required for logged exceptions to be forwarded to StackDriver Error Reporting.                                                                                 |
| UseSourceContextAsLogName | The log name for a log entry will be set to the [SourceContext](https://github.com/serilog/serilog/wiki/Writing-Log-Events#source-contexts) property if available. Default is `true`.                                   |
| UseLogCorrelation         | Integrate logs with [Cloud Trace](https://cloud.google.com/trace) by setting `Trace`, `SpanId`, `TraceSampled` properties if available. Default is `true`.                                                              |
| GoogleCredentialJson      | Override [Application Default Credentials](https://cloud.google.com/docs/authentication/production#providing_credentials_to_your_application) with the content of a JSON credential file.                               |

### Log Level Mapping

This table shows the mapping from Serilog [`LogLevel`](https://github.com/serilog/serilog/wiki/Configuration-Basics#minimum-level) to Google Cloud Logging [`LogSeverity`](https://cloud.google.com/logging/docs/reference/v2/rest/v2/LogEntry#LogSeverity)

| Serilog     | Cloud Logging |
| ----------- | ------------- |
| Verbose     | Debug         |
| Debug       | Debug         |
| Information | Info          |
| Warning     | Warning       |
| Error       | Error         |
| Fatal       | Critical      |
