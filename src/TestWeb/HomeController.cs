using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Serilog;

namespace TestWeb
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ILoggerFactory _loggerFactory;

        public HomeController(ILogger<HomeController> logger, ILoggerFactory loggerFactory)
        {
            _logger = logger;
            _loggerFactory = loggerFactory;
        }

        public IActionResult Index()
        {
            Log.Information("Test info message with serilog");
            Log.Debug("Test debug message with serilog");

            _logger.LogInformation("Test info message with ILogger abstraction");
            _logger.LogDebug("Test debug message with ILogger abstraction");

            _logger.LogInformation(eventId: new Random().Next(), message: "Testing message with random event ID");
            _logger.LogInformation("Test message with List {list}", new List<string> { "foo", "123", "bar", "456" });
            _logger.LogInformation("Test message with Dictionary {dict}", new Dictionary<string, string>
            {
                { "myKey", "myValue" },
                { "mySecondKey", "withAValue" }
            });

            // ASP.NET Logger Factory accepts custom log names but these must follow the rules for Google Cloud logging:
            // https://cloud.google.com/logging/docs/reference/v2/rest/v2/LogEntry
            // Names must only include upper and lower case alphanumeric characters, forward-slash, underscore, hyphen, and period. No spaces!
            var logger = _loggerFactory.CreateLogger("AnotherLogger");
            logger.LogInformation("Test info message with ILoggerFactory abstraction and custom log name");

            var url = $"https://console.cloud.google.com/logs/viewer?project={Program.GCP_PROJECT_ID}";
            var page = $"<html><body>Logged messages, visit GCP log viewer at <a href='{url}'>{url}</a></body></html>";
            return Content(page, "text/html");
        }
    }
}
