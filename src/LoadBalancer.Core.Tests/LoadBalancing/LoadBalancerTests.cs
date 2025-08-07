namespace LoadBalancerProject.Tests.LoadBalancing
{
    using LoadBalancerProject.Health;
    using LoadBalancerProject.LoadBalancing;
    using LoadBalancerProject.LoadBalancing.Strategies;
    using LoadBalancerProject.Strategies;
    using LoadBalancerProject.Tests.Common;
    using Microsoft.Extensions.Logging;
    using NSubstitute;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;

    [TestFixture]
    public class LoadBalancerTests
    {
        [Test]
        public void Constructor_NullLogger_ThrowsArgumentNullException()
        {
            // Arrange
            var health = Substitute.For<IHealthChecker>();
            var strategies = Substitute.For<IStrategyProvider>();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(
                () => new LoadBalancer(null!, health, strategies));
        }

        [Test]
        public void Constructor_NullHealthChecker_ThrowsArgumentNullException()
        {
            // Arrange
            var logger = Substitute.For<ILogger<LoadBalancer>>();
            var strategies = Substitute.For<IStrategyProvider>();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(
                () => new LoadBalancer(logger, null!, strategies));
        }

        [Test]
        public void Constructor_NullStrategyProvider_ThrowsArgumentNullException()
        {
            // Arrange
            var logger = Substitute.For<ILogger<LoadBalancer>>();
            var health = Substitute.For<IHealthChecker>();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(
                () => new LoadBalancer(logger, health, null!));
        }

        [Test]
        public void SelectBackend_NoHealthy_ThrowsInvalidOperationAndLogsWarning()
        {
            // Arrange
            var logger = Substitute.For<ILogger<LoadBalancer>>();
            var health = Substitute.For<IHealthChecker>();
            var strategies = Substitute.For<IStrategyProvider>();
            health.GetHealthyBackends().Returns(new List<Uri>());
            var sut = new LoadBalancer(logger, health, strategies);

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => sut.SelectBackend());
            Assert.That(ex!.Message, Does.Contain("No healthy backends"));
            logger.AssertLogContains(LogLevel.Warning, "No healthy backends");
        }

        [Test]
        public void SelectBackend_WithHealthy_UsesStrategyAndLogsDebug()
        {
            // Arrange
            var logger = Substitute.For<ILogger<LoadBalancer>>();
            var health = Substitute.For<IHealthChecker>();
            var strategies = Substitute.For<IStrategyProvider>();
            var backends = new List<Uri> { new Uri("tcp://host:1") };
            var strategy = Substitute.For<IBackendSelectorStrategy>();
            strategies.Current.Returns(strategy);
            health.GetHealthyBackends().Returns(backends);
            strategy.SelectBackend(backends).Returns(backends[0]);
            var sut = new LoadBalancer(logger, health, strategies);

            // Act
            var selected = sut.SelectBackend();

            // Assert
            Assert.That(selected, Is.EqualTo(backends[0]));
            strategy.Received(1).SelectBackend(backends);
            logger.AssertLogContains(LogLevel.Debug, "Selected backend");
        }
    }
}
