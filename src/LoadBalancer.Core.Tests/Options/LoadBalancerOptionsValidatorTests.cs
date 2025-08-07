namespace LoadBalancerProject.Tests.Options
{
    using LoadBalancerProject.Options;
    using LoadBalancerProject.Tests.Common;
    using Microsoft.Extensions.Logging;
    using NSubstitute;
    using NUnit.Framework;
    using System.Collections.Generic;

    [TestFixture]
    public class LoadBalancerOptionsValidatorTests
    {
        [Test]
        public void Validate_WithValidMinimalOptions_ReturnsSuccessAndLogsDebug()
        {
            // Arrange
            var logger = Substitute.For<ILogger<LoadBalancerOptionsValidator>>();
            var sut = new LoadBalancerOptionsValidator(logger);
            var opts = new LoadBalancerOptions
            {
                Backends = new List<string> { "tcp://localhost:5001" },
                HealthCheckIntervalSeconds = 5,
                Strategy = "RoundRobin",
                ListenPort = 6000
            };

            // Act
            var result = sut.Validate("name", opts);

            // Assert
            Assert.That(result.Succeeded, Is.True);
            logger.AssertLogContains(LogLevel.Debug, "validated successfully");
        }

        [Test]
        public void Validate_WithUnknownStrategy_ReturnsFailureAndLogsError()
        {
            // Arrange
            var logger = Substitute.For<ILogger<LoadBalancerOptionsValidator>>();
            var sut = new LoadBalancerOptionsValidator(logger);
            var opts = new LoadBalancerOptions
            {
                Backends = new List<string> { "tcp://localhost:5001" },
                Strategy = "Nope",
                HealthCheckIntervalSeconds = 5,
                ListenPort = 6000
            };

            // Act
            var result = sut.Validate("name", opts);

            // Assert
            Assert.That(result.Failed, Is.True);
            logger.AssertLogContains(LogLevel.Error, "validation failed");
        }

        [Test]
        public void Validate_WithCaseInsensitiveStrategy_LeastQueue_Succeeds()
        {
            // Arrange
            var logger = Substitute.For<ILogger<LoadBalancerOptionsValidator>>();
            var sut = new LoadBalancerOptionsValidator(logger);
            var opts = new LoadBalancerOptions
            {
                Backends = new List<string> { "tcp://localhost:5001" },
                Strategy = "leastqueue",
                HealthCheckIntervalSeconds = 5,
                ListenPort = 6000
            };

            // Act
            var result = sut.Validate("name", opts);

            // Assert
            Assert.That(result.Succeeded, Is.True);
        }

        [Test]
        public void Validate_WithInvalidListenPort_Fails()
        {
            // Arrange
            var logger = Substitute.For<ILogger<LoadBalancerOptionsValidator>>();
            var sut = new LoadBalancerOptionsValidator(logger);
            var opts = new LoadBalancerOptions
            {
                Backends = new List<string> { "tcp://localhost:5001" },
                Strategy = "RoundRobin",
                HealthCheckIntervalSeconds = 5,
                ListenPort = 0
            };

            // Act
            var result = sut.Validate("name", opts);

            // Assert
            Assert.That(result.Failed, Is.True);
            logger.AssertLogContains(LogLevel.Error, "validation failed");
        }

        [Test]
        public void Validate_WithOutOfRangeHealthInterval_Fails()
        {
            // Arrange
            var logger = Substitute.For<ILogger<LoadBalancerOptionsValidator>>();
            var sut = new LoadBalancerOptionsValidator(logger);
            var opts = new LoadBalancerOptions
            {
                Backends = new List<string> { "tcp://localhost:5001" },
                Strategy = "RoundRobin",
                HealthCheckIntervalSeconds = 0,
                ListenPort = 6000
            };

            // Act
            var result = sut.Validate("name", opts);

            // Assert
            Assert.That(result.Failed, Is.True);
            logger.AssertLogContains(LogLevel.Error, "validation failed");
        }

        [Test]
        public void Validate_WithEmptyBackends_Fails()
        {
            // Arrange
            var logger = Substitute.For<ILogger<LoadBalancerOptionsValidator>>();
            var sut = new LoadBalancerOptionsValidator(logger);
            var opts = new LoadBalancerOptions
            {
                Backends = new List<string>(),
                Strategy = "RoundRobin",
                HealthCheckIntervalSeconds = 5,
                ListenPort = 6000
            };

            // Act
            var result = sut.Validate("name", opts);

            // Assert
            Assert.That(result.Failed, Is.True);
            logger.AssertLogContains(LogLevel.Error, "validation failed");
        }

        [Test]
        public void Validate_WithInvalidBackendUris_CollectsFailuresAndFails()
        {
            // Arrange
            var logger = Substitute.For<ILogger<LoadBalancerOptionsValidator>>();
            var sut = new LoadBalancerOptionsValidator(logger);
            var opts = new LoadBalancerOptions
            {
                Backends = new List<string> { "", "http://host:1", "tcp://:99999", "nonsense" },
                Strategy = "RoundRobin",
                HealthCheckIntervalSeconds = 5,
                ListenPort = 6000
            };

            // Act
            var result = sut.Validate("name", opts);

            // Assert
            Assert.That(result.Failed, Is.True);
            logger.AssertLogContains(LogLevel.Error, "validation failed");
        }
    }
}
