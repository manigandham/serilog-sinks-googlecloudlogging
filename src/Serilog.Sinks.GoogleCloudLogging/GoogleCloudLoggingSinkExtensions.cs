﻿using System;
using System.Collections.Generic;
using Serilog.Configuration;
using Serilog.Formatting.Display;

namespace Serilog.Sinks.GoogleCloudLogging
{
    public static class GoogleCloudLoggingSinkExtensions
    {
        public static LoggerConfiguration GoogleCloudLogging(this LoggerSinkConfiguration loggerConfiguration,
            GoogleCloudLoggingSinkOptions sinkOptions,
            int? batchSizeLimit = null,
            TimeSpan? period = null,
            string outputTemplate = null
        )
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

        public static LoggerConfiguration GoogleCloudLogging(
            this LoggerSinkConfiguration loggerConfiguration,
            string projectId,
            string resourceType = null,
            string logName = null,
            Dictionary<string, string> labels = null,
            Dictionary<string, string> resourceLabels = null,
            bool useSourceContextAsLogName = true,
            bool useJsonOutput = false,
            string googleCredentialJson = null,
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
                googleCredentialJson
            );

            return loggerConfiguration.GoogleCloudLogging(options, batchSizeLimit, period, outputTemplate);
        }
    }
}
