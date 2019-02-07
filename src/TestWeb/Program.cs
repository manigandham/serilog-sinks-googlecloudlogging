using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
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

            var options = new GoogleCloudLoggingSinkOptions(GCP_PROJECT_ID);
            options.UseSourceContextAsLogName = false;
            options.UseJsonOutput = true;

            Log.Logger = new LoggerConfiguration()
                .WriteTo.GoogleCloudLogging(options) // Add this to send Serilog output to GCP
                .MinimumLevel.Is(LogEventLevel.Verbose) // Serilog defaults to Info level and above, use this to override
                .CreateLogger();

            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureServices(services => services.AddMvc())
                .Configure(app =>
                {
                    app.UseDeveloperExceptionPage();
                    app.UseMvcWithDefaultRoute();
                })
                .UseSerilog() // Add this to send all built-in logging to Serilog
                .Build();
    }
}
