using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using Xunit;

namespace Serilog.Sinks.GoogleCloudLogging.Test
{
    public class GoogleCloudLoggingSinkOptionsTest
    {
        [Fact]
        public void WillInitializeWithDefaultArgumentsCorrectly()
        {
            new GoogleCloudLoggingSinkOptions()
                .Should()
                .BeEquivalentTo(new GoogleCloudLoggingSinkOptions
                {
                    GoogleCredentialJson = null,
                    Labels = { },
                    LogName = "Default",
                    ProjectId = null,
                    ResourceLabels = { },
                    ResourceType = null,
                    ServiceName = null,
                    ServiceVersion = "<Unknown>",
                    UseJsonOutput = false,
                    UseSourceContextAsLogName = true,
                });
        }

        [Fact]
        public void WillCorrectlyAssignAllConstructorArguments()
        {
            var options = new GoogleCloudLoggingSinkOptions(projectId: "projectId",
                resourceType: "k8s_pod",
                logName: "logName",
                labels: new Dictionary<string, string> { { "labelKey", "label value" } },
                resourceLabels: new Dictionary<string, string> { { "resourceKey", "resource value" } },
                useSourceContextAsLogName: false,
                useJsonOutput: true,
                googleCredentialJson: "{}",
                serviceName: "service-name",
                serviceVersion: "1.0.1");

            options
                .Should()
                .BeEquivalentTo(new GoogleCloudLoggingSinkOptions
                {
                    GoogleCredentialJson = "{}",
                    Labels = { { "labelKey", "label value" } },
                    LogName = "logName",
                    ProjectId = "projectId",
                    ResourceLabels = { { "resourceKey", "resource value" } },
                    ResourceType = "k8s_pod",
                    ServiceName = "service-name",
                    ServiceVersion = "1.0.1",
                    UseJsonOutput = true,
                    UseSourceContextAsLogName = false,
                });
        }
    }
}
