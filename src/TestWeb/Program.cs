using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Serilog;
using Serilog.Sinks.GoogleCloudLogging;

namespace TestWeb
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.GoogleCloudLogging(new GoogleCloudLoggingSinkOptions("PROJECTID"))
                .CreateLogger();
            
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                   .UseStartup<Startup>()
                   .UseSerilog()
                   .Build();
    }
}
