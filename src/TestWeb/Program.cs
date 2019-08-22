using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.GoogleCloudLogging;

namespace TestWeb
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // enable serilog to log out internal messages to console for debugging
            Serilog.Debugging.SelfLog.Enable(Console.WriteLine);

            // sink can be configured from appsettings.json or using code, both examples in methods below
            SetOptionsFromAppSettings();
            // SetOptionsProgrammatically();

            BuildWebHost(args).Run();
        }

        private static void SetOptionsFromAppSettings()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();
        }

        private static void SetOptionsProgrammatically()
        {
            var options = new GoogleCloudLoggingSinkOptions("PROJECT-ID-HERE-12345")
            {
                ResourceType = "k8s_cluster",
                LogName = "someLogName",
                UseSourceContextAsLogName = true,
                UseJsonOutput = true,
                WriteOriginalFormat = true
            };

            Log.Logger = new LoggerConfiguration()
                .WriteTo.GoogleCloudLogging(options) // Add this to send Serilog output to GCP
                .MinimumLevel.Is(LogEventLevel.Verbose) // Serilog defaults to Info level and above, use this to override
                .CreateLogger();
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
