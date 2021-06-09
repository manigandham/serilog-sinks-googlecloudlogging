using System;
using System.Collections.Generic;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;
using Serilog.Sinks.PeriodicBatching;

namespace Serilog.Sinks.GoogleCloudLogging
{
    public static class GoogleCloudLoggingSinkExtensions
    {
        /// <summary>
        /// Writes log events to Google Cloud Logging.
        /// </summary>
        /// <param name="loggerConfiguration">Logger sink configuration.</param>
        /// <param name="sinkOptions">Google Cloud Logging sink options.</param>
        /// <param name="batchSizeLimit">The maximum number of events to include in a single batch. The default is 100.</param>
        /// <param name="period">The time to wait between checking for event batches. The default is five seconds.</param>
        /// <param name="queueLimit">Maximum number of events in the queue. If not specified, uses an unbounded queue.</param>
        /// <param name="outputTemplate">A message template describing the format used to write to the sink.</param>
        /// <param name="textFormatter">Controls the rendering of the log events.</param>
        /// <param name="restrictedToMinimumLevel">The minimum level for events passed through the sink. Ignored when <paramref name="levelSwitch"/> is specified.</param>
        /// <param name="levelSwitch">A switch allowing the pass-through minimum level to be changed at runtime.</param>
        /// <returns>Configuration object allowing method chaining.</returns>
        public static LoggerConfiguration GoogleCloudLogging(
            this LoggerSinkConfiguration loggerConfiguration,
            GoogleCloudLoggingSinkOptions sinkOptions,
            int? batchSizeLimit = null,
            TimeSpan? period = null,
            int? queueLimit = null,
            string? outputTemplate = null,
            ITextFormatter? textFormatter = null,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            LoggingLevelSwitch? levelSwitch = null)
        {
            var batchingOptions = new PeriodicBatchingSinkOptions
            {
                BatchSizeLimit = batchSizeLimit ?? 100,
                Period = period ?? TimeSpan.FromSeconds(5),
                QueueLimit = queueLimit
            };

            var formatter = outputTemplate != null ? new MessageTemplateTextFormatter(outputTemplate) : textFormatter;
            var sink = new GoogleCloudLoggingSink(sinkOptions, formatter);
            var batchingSink = new PeriodicBatchingSink(sink, batchingOptions);

            return loggerConfiguration.Sink(batchingSink, restrictedToMinimumLevel, levelSwitch);
        }

        /// <summary>
        /// Overload that accepts all configuration settings as parameters to allow configuration in files using serilog-settings-configuration package.
        /// This method creates a GoogleCloudLoggingSinkOptions and calls the standard constructor above.
        /// </summary>
        /// <returns>Configuration object allowing method chaining.</returns>
        public static LoggerConfiguration GoogleCloudLogging(
            this LoggerSinkConfiguration loggerConfiguration,
            string? projectId = null,
            string? resourceType = null,
            string? logName = null,
            ITextFormatter? formatter = null,
            Dictionary<string, string>? labels = null,
            Dictionary<string, string>? resourceLabels = null,
            bool useSourceContextAsLogName = true,
            bool useJsonOutput = false,
            bool useLogCorrelation = true,
            string? googleCredentialJson = null,
            string? serviceName = null,
            string? serviceVersion = null,
            int? batchSizeLimit = null,
            TimeSpan? period = null,
            int? queueLimit = null,
            string? outputTemplate = null,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            LoggingLevelSwitch? levelSwitch = null)
        {
            var options = new GoogleCloudLoggingSinkOptions(
                projectId,
                resourceType,
                logName,
                labels,
                resourceLabels,
                useSourceContextAsLogName,
                useJsonOutput,
                useLogCorrelation,
                googleCredentialJson,
                serviceName,
                serviceVersion
            );

            return loggerConfiguration.GoogleCloudLogging(
                options,
                batchSizeLimit,
                period,
                queueLimit,
                outputTemplate,
                formatter,
                restrictedToMinimumLevel,
                levelSwitch
            );
        }
    }
}
