using System;
using System.Collections.Generic;
using Serilog.Configuration;
using Serilog.Formatting.Display;
using Serilog.Sinks.PeriodicBatching;

namespace Serilog.Sinks.GoogleCloudLogging
{
    public static class GoogleCloudLoggingSinkExtensions
    {
        public static LoggerConfiguration GoogleCloudLogging(
            this LoggerSinkConfiguration loggerConfiguration,
            GoogleCloudLoggingSinkOptions sinkOptions,
            int? batchSizeLimit = null,
            TimeSpan? period = null,
            string outputTemplate = null)
        {
            var messageTemplateTextFormatter = String.IsNullOrWhiteSpace(outputTemplate) ? null : new MessageTemplateTextFormatter(outputTemplate, null);

            var sink = new GoogleCloudLoggingSink(
                sinkOptions,
                messageTemplateTextFormatter
            );

            var batchingOptions = new PeriodicBatchingSinkOptions
            {
                BatchSizeLimit = batchSizeLimit ?? 100,
                Period = period ?? TimeSpan.FromSeconds(5)
            };

            var batchingSink = new PeriodicBatchingSink(sink, batchingOptions);

            return loggerConfiguration.Sink(batchingSink);
        }

        /// <summary>
        /// Overload that accepts all configuration settings as parameters to allow configuration in files using serilog-settings-configuration package.
        /// This method creates a GoogleCloudLoggingSinkOptions and calls the standard constructor above.
        /// </summary>
        public static LoggerConfiguration GoogleCloudLogging(
            this LoggerSinkConfiguration loggerConfiguration,
            string projectId = null,
            string resourceType = null,
            string logName = null,
            Dictionary<string, string> labels = null,
            Dictionary<string, string> resourceLabels = null,
            bool useSourceContextAsLogName = true,
            bool useJsonOutput = false,
            string googleCredentialJson = null,
            string serviceName = null,
            string serviceVersion = null,
            int? batchSizeLimit = null,
            TimeSpan? period = null,
            string outputTemplate = null
        )
        {
            var options = new GoogleCloudLoggingSinkOptions(
                projectId,
                resourceType,
                logName,
                labels,
                resourceLabels,
                useSourceContextAsLogName,
                useJsonOutput,
                googleCredentialJson,
                serviceName,
                serviceVersion
            );

            return loggerConfiguration.GoogleCloudLogging(options, batchSizeLimit, period, outputTemplate);
        }
    }
}
