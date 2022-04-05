using System;
using System.Collections.Generic;
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
                    ServiceVersion = null,
                    UseSourceContextAsLogName = true,
                    UseLogCorrelation = true,
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
                useLogCorrelation: false,
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
                    UseSourceContextAsLogName = false,
                    UseLogCorrelation = false
                });
        }

        [Fact]
        public void CheckNullLogName()
        {
            var options = new GoogleCloudLoggingSinkOptions("projectId")
            {
                LogName = null!
            };

            Assert.Throws<ArgumentNullException>(() => new GoogleCloudLoggingSink(options, null));
        }
    }
}
