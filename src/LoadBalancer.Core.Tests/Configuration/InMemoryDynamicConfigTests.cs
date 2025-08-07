namespace LoadBalancerProject.Tests.Configuration
{
    using LoadBalancerProject.Configuration;
    using LoadBalancerProject.Tests.Common;
    using Microsoft.Extensions.Logging;
    using NSubstitute;
    using NUnit.Framework;

    [TestFixture]
    public class InMemoryDynamicConfigTests
    {
        [Test]
        public void Strategy_GetAfterConstruction_ReturnsInitialValue()
        {
            // Arrange
            var logger = Substitute.For<ILogger<InMemoryDynamicConfig>>();
            var sut = new InMemoryDynamicConfig("RoundRobin", 5, logger);

            // Act
            var value = sut.Strategy;

            // Assert
            Assert.That(value, Is.EqualTo("RoundRobin"));
        }

        [Test]
        public void HealthCheckIntervalSeconds_GetAfterConstruction_ReturnsInitialValue()
        {
            // Arrange
            var logger = Substitute.For<ILogger<InMemoryDynamicConfig>>();
            var sut = new InMemoryDynamicConfig("LeastQueue", 42, logger);

            // Act
            var value = sut.HealthCheckIntervalSeconds;

            // Assert
            Assert.That(value, Is.EqualTo(42));
        }

        [Test]
        public void Strategy_SetNewValue_UpdatesAndLogsInformation()
        {
            // Arrange
            var logger = Substitute.For<ILogger<InMemoryDynamicConfig>>();
            var sut = new InMemoryDynamicConfig("A", 1, logger);

            // Act
            sut.Strategy = "B";

            // Assert
            Assert.That(sut.Strategy, Is.EqualTo("B"));
            logger.AssertLogContains(LogLevel.Information, "Strategy updated to");
            logger.AssertLogContains(LogLevel.Information, "B");
        }

        [Test]
        public void Strategy_SetNull_UpdatesAndLogsInformation()
        {
            // Arrange
            var logger = Substitute.For<ILogger<InMemoryDynamicConfig>>();
            var sut = new InMemoryDynamicConfig("Initial", 1, logger);

            // Act
            sut.Strategy = null!;

            // Assert
            Assert.That(sut.Strategy, Is.Null);
            logger.AssertLogContains(LogLevel.Information, "Strategy updated to");
        }

        [Test]
        public void Strategy_SetMultipleTimes_ReflectsLatestValueAndLogsEachTime()
        {
            // Arrange
            var logger = Substitute.For<ILogger<InMemoryDynamicConfig>>();
            var sut = new InMemoryDynamicConfig("S0", 1, logger);

            // Act
            sut.Strategy = "S1";
            sut.Strategy = "S2";
            sut.Strategy = "S3";

            // Assert
            Assert.That(sut.Strategy, Is.EqualTo("S3"));
            Assert.That(logger.CountLogs(LogLevel.Information, "Strategy updated to"), Is.GreaterThanOrEqualTo(3));
        }

        [Test]
        public void HealthCheckIntervalSeconds_SetNewValue_UpdatesAndLogsInformation()
        {
            // Arrange
            var logger = Substitute.For<ILogger<InMemoryDynamicConfig>>();
            var sut = new InMemoryDynamicConfig("X", 10, logger);

            // Act
            sut.HealthCheckIntervalSeconds = 15;

            // Assert
            Assert.That(sut.HealthCheckIntervalSeconds, Is.EqualTo(15));
            logger.AssertLogContains(LogLevel.Information, "Health check interval updated to");
            logger.AssertLogContains(LogLevel.Information, "15");
        }

        [Test]
        public void HealthCheckIntervalSeconds_SetMultipleTimes_ReflectsLatestValueAndLogsEachTime()
        {
            // Arrange
            var logger = Substitute.For<ILogger<InMemoryDynamicConfig>>();
            var sut = new InMemoryDynamicConfig("X", 1, logger);

            // Act
            sut.HealthCheckIntervalSeconds = 2;
            sut.HealthCheckIntervalSeconds = 3;
            sut.HealthCheckIntervalSeconds = 4;

            // Assert
            Assert.That(sut.HealthCheckIntervalSeconds, Is.EqualTo(4));
            Assert.That(logger.CountLogs(LogLevel.Information, "Health check interval updated to"), Is.GreaterThanOrEqualTo(3));
        }
    }
}
