namespace LoadBalancerProject.Tests.Options
{
    using LoadBalancerProject.Options;
    using LoadBalancerProject.Tests.Common;
    using Microsoft.Extensions.Logging;
    using NSubstitute;
    using NUnit.Framework;

    [TestFixture]
    public class TcpForwarderOptionsValidatorTests
    {
        [Test]
        public void Validate_WithDefaultOptions_ReturnsSuccessAndLogsDebug()
        {
            // Arrange
            var logger = Substitute.For<ILogger<TcpForwarderOptionsValidator>>();
            var sut = new TcpForwarderOptionsValidator(logger);
            var opts = new TcpForwarderOptions();

            // Act
            var result = sut.Validate("name", opts);

            // Assert
            Assert.That(result.Succeeded, Is.True);
            logger.AssertLogContains(LogLevel.Debug, "validated successfully");
        }

        [Test]
        public void Validate_WithOutOfRangeValues_FailsAndLogsError()
        {
            // Arrange
            var logger = Substitute.For<ILogger<TcpForwarderOptionsValidator>>();
            var sut = new TcpForwarderOptionsValidator(logger);
            var opts = new TcpForwarderOptions
            {
                MaxConcurrentConnections = 0,
                IdleTimeoutSeconds = 0,
                MaxLifetimeSeconds = 0,
                BufferSize = 100
            };

            // Act
            var result = sut.Validate("name", opts);

            // Assert
            Assert.That(result.Failed, Is.True);
            logger.AssertLogContains(LogLevel.Error, "validation failed");
        }

        [Test]
        public void Validate_MaxLifetimeLessThanIdleTimeout_Fails()
        {
            // Arrange
            var logger = Substitute.For<ILogger<TcpForwarderOptionsValidator>>();
            var sut = new TcpForwarderOptionsValidator(logger);
            var opts = new TcpForwarderOptions
            {
                MaxConcurrentConnections = 10,
                IdleTimeoutSeconds = 100,
                MaxLifetimeSeconds = 50,
                BufferSize = 2048
            };

            // Act
            var result = sut.Validate("name", opts);

            // Assert
            Assert.That(result.Failed, Is.True);
        }

        [Test]
        public void Validate_BoundaryValuesAtLimits_Succeeds()
        {
            // Arrange
            var logger = Substitute.For<ILogger<TcpForwarderOptionsValidator>>();
            var sut = new TcpForwarderOptionsValidator(logger);
            var opts = new TcpForwarderOptions
            {
                MaxConcurrentConnections = 1,
                IdleTimeoutSeconds = 1,
                MaxLifetimeSeconds = 86400,
                BufferSize = 1024
            };

            // Act
            var result = sut.Validate("name", opts);

            // Assert
            Assert.That(result.Succeeded, Is.True);
        }

        [Test]
        public void Validate_BufferSizeUpperBound_Succeeds()
        {
            // Arrange
            var logger = Substitute.For<ILogger<TcpForwarderOptionsValidator>>();
            var sut = new TcpForwarderOptionsValidator(logger);
            var opts = new TcpForwarderOptions
            {
                MaxConcurrentConnections = 1,
                IdleTimeoutSeconds = 10,
                MaxLifetimeSeconds = 10,
                BufferSize = 4 * 1024 * 1024
            };

            // Act
            var result = sut.Validate("name", opts);

            // Assert
            Assert.That(result.Succeeded, Is.True);
        }
    }
}
