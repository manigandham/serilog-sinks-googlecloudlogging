using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.GoogleCloudLogging;

// enable serilog to log out internal messages to console for debugging
Serilog.Debugging.SelfLog.Enable(Console.WriteLine);

// see serilog .net 6 integration: https://blog.datalust.co/using-serilog-in-net-6/
Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();
Log.Information("Starting up");

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Services.AddControllersWithViews();

    // serilog config from app settings
    {
        // var configuration = new ConfigurationBuilder()
        //     .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
        //     .Build();
        //
        //  builder.Host.UseSerilog((ctx, lc) => lc.ReadFrom.Configuration(ctx.Configuration));
    }

    // serilog config programmatically
    {
        var options = new GoogleCloudLoggingSinkOptions
        {
            ProjectId = "PROJECT-ID-12345",
            ResourceType = "gce_instance",
            LogName = "someLogName",
            UseSourceContextAsLogName = true,
        };

        builder.Host.UseSerilog((ctx, lc) => lc.WriteTo.Console().WriteTo.GoogleCloudLogging(options).MinimumLevel.Is(LogEventLevel.Verbose));
    }


    var app = builder.Build();
    app.UseSerilogRequestLogging();

    app.MapGet("/", ([FromServices] Microsoft.Extensions.Logging.ILogger<Program> _logger, [FromServices] ILoggerFactory _loggerFactory) =>
    {
        Log.Information("Test info message with serilog");
        Log.Debug("Test debug message with serilog");

        _logger.LogInformation("Test info message with ILogger abstraction");
        _logger.LogDebug("Test debug message with ILogger abstraction");

        // ASP.NET Logger Factory accepts custom log names but these must follow the rules for Google Cloud logging:
        // https://cloud.google.com/logging/docs/reference/v2/rest/v2/LogEntry
        // Names must only include upper and lower case alphanumeric characters, forward-slash, underscore, hyphen, and period. No spaces!
        var anotherLogger = _loggerFactory.CreateLogger("AnotherLogger");
        anotherLogger.LogInformation("Test message with ILoggerFactory abstraction and custom log name");

        _logger.LogInformation(eventId: new Random().Next(), message: "Test message with random event ID");
        _logger.LogInformation("Test message with List<string> {list}", new List<string> { "foo", "bar", "pizza" });
        _logger.LogInformation("Test message with List<int> {list}", new List<int> { 123, 456, 7890 });
        _logger.LogInformation("Test message with Dictionary<string,object> {dict}", new Dictionary<string, object>
            {
                { "valueAsNull", null },
                { "valueAsBool", true },
                { "valueAsString", "qwerty" },
                { "valueAsStringNumber", "00000" },
                { "valueAsMaxInt", int.MaxValue },
                { "valueAsMaxLong", long.MaxValue },
                { "valueAsDouble", 123.456 },
                { "valueAsMaxDouble", double.MaxValue },
                { "valueAsMaxDecimal", decimal.MaxValue },
            });

        try
        {
            throw new Exception("Testing exception logging");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "");
        }

        var url = $"https://console.cloud.google.com/logs/viewer";
        var html = $"<html><body>Logged messages at {DateTime.UtcNow:O}, visit GCP log viewer at <a href='{url}' target='_blank'>{url}</a></body></html>";

        return Results.Content(html, "text/html");
    });

    app.Run();

}
catch (Exception ex)
{
    Log.Fatal(ex, "Unhandled exception");
}
finally
{
    Log.Information("Shut down complete");
    Log.CloseAndFlush();
}
