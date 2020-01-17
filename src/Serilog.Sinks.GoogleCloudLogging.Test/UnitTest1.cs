using FluentAssertions;
using Xunit;

namespace Serilog.Sinks.GoogleCloudLogging.Test
{
    public class UnitTest1
    {
        [Fact]
        public void ShouldPass()
        {
            Assert.Equal("key", "key");
        }

        [Fact]
        public void ShouldFail()
        {
            Assert.Equal("key", "value");
        }

        [Fact]
        public void FluentShouldPass()
        {
            var user = new User
            {
                Name = "Bob",
                Age = 25,
            };

            user.Should().BeEquivalentTo(new User
            {
                Name = "Bob",
                Age = 25,
            });
        }

        [Fact]
        public void FluentShouldFail()
        {
            var user = new User
            {
                Name = "Bob",
                Age = 25,
            };

            user.Should().BeEquivalentTo(new User
            {
                Name = "Sue",
                Age = 25,
            });
        }

        public class User
        {
            public string Name { get; set; }

            public int Age { get; set; }
        }
    }
}
