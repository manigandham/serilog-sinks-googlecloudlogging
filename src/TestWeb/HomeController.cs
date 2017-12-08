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

        public string Index()
        {
            Log.Information("Testing info message with serilog");
            Log.Debug("Testing debug message with serilog");

            _logger.LogInformation("Testing info message with ILogger abstraction");
            _logger.LogDebug("Testing debug message with ILogger abstraction");
            _logger.LogDebug(eventId: new Random().Next(), message: "Testing message with random event ID");
            _logger.LogInformation("Test message with a Dictionary {myDict}", new Dictionary<string, string>
            {
                { "myKey", "myValue" },
                { "mySecondKey", "withAValue" }
            });

            // ASP.NET Logger Factor accepts string log names, but these must follow the rules for Google Cloud logging:
            // https://cloud.google.com/logging/docs/reference/v2/rest/v2/LogEntry
            // Names must only include upper and lower case alphanumeric characters, forward-slash, underscore, hyphen, and period. No spaces!
            var logger = _loggerFactory.CreateLogger("AnotherLogger");
            logger.LogInformation("Testing info message with ILoggerFactor abstraction and custom log name");

            return $"Logged messages, visit GCP log viewer at https://console.cloud.google.com/logs/viewer?project={Program.GCP_PROJECT_ID}";
        }
    }
}
