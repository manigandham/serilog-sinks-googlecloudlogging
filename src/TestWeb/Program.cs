using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.GoogleCloudLogging;

namespace TestWeb
{
    public class Program
    {
        // GCP Project ID (not the display name). 
        // Google Cloud Dotnet libraries use the default service account on the VM (or authenticated via gcloud SDk).
        // The service account must have the "Logs Writer" permission enabled to send logs.
        public const string GCP_PROJECT_ID = "PROJECT-ID-HERE-12345";

        public static void Main(string[] args)
        {
            Serilog.Debugging.SelfLog.Enable(msg => Console.WriteLine(msg));

            Log.Logger = new LoggerConfiguration()
                .WriteTo.GoogleCloudLogging(new GoogleCloudLoggingSinkOptions(GCP_PROJECT_ID)) // Add this to send Serilog output to GCP
                .MinimumLevel.Is(LogEventLevel.Verbose) // Serilog defaults to Info level and above, use this to override
                .CreateLogger();

            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                   .UseStartup<Startup>()
                   .UseSerilog() // Add this to send all built-in logging to Serilog
                   .Build();
    }
}
