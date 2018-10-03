using System;
using Serilog.Configuration;
using Serilog.Formatting.Display;

namespace Serilog.Sinks.GoogleCloudLogging
{
    public static class GoogleCloudLoggingSinkExtensions
    {
        public static LoggerConfiguration GoogleCloudLogging(this LoggerSinkConfiguration loggerConfiguration, GoogleCloudLoggingSinkOptions sinkOptions, int? batchSizeLimit = null, TimeSpan? period = null, string outputTemplate = null)
        {
            var messageTemplateTextFormatter = String.IsNullOrWhiteSpace(outputTemplate) ? null : new MessageTemplateTextFormatter(outputTemplate, null);

            var sink = new GoogleCloudLoggingSink(
                sinkOptions,
                messageTemplateTextFormatter,
                batchSizeLimit ?? 100,
                period ?? TimeSpan.FromSeconds(5)
            );

            return loggerConfiguration.Sink(sink);
        }

        public static LoggerConfiguration GoogleCloudLogging(this LoggerSinkConfiguration loggerConfiguration, string projectID, int? batchSizeLimit = null, TimeSpan? period = null, string outputTemplate = null)
        {
            return loggerConfiguration.GoogleCloudLogging(new GoogleCloudLoggingSinkOptions(projectID),batchSizeLimit,period);
        }
    }
}
