# Serilog.Sinks.GoogleCloudLogging

Serilog sink that writes events to [Google Cloud Platform Stackdriver Logging](https://cloud.google.com/logging/).

## Getting started

Install from Nuget:

     Install-Package Serilog.Sinks.GoogleCloudLogging

Configure 

```csharp
var log = new LoggerConfiguration()
    .WriteTo.GoogleCloudLogging(new GoogleCloudLoggingSinkOptions("YOUR_PROJECT_ID"))
    .CreateLogger();
```

Sink options:
- Project ID - **Required** Google Cloud project ID which will hold logs.
- Resource Type - Resource type for logs, defaults to "global".
- Log Name - Name of individual log, will use SourceContext from Serilog automatically or fallback to this setting, defaults to "Default".

