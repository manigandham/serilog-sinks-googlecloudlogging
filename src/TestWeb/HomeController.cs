using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Serilog;

namespace TestWeb
{
    public class HomeController : Controller
    {
        ILoggerFactory _loggerFactory;

        public HomeController(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }

        public string Index()
        {
            Log.Information("Testing info message with serilog");
            Log.Debug("Testing debug message with serilog");
            
            var logger = _loggerFactory.CreateLogger("Logger Factory");
            logger.LogInformation("Testing info message with abstraction");
            logger.LogDebug("Testing debug message with abstraction");
            logger.LogDebug(eventId: new Random().Next(), message: "Testing message with random ID");

            return "OK";
        }
    }
}
