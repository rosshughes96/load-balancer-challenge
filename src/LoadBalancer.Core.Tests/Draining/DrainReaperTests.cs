namespace LoadBalancerProject.Tests.Draining
{
    using LoadBalancerProject.Backends;
    using LoadBalancerProject.Draining;
    using LoadBalancerProject.Metrics;
    using LoadBalancerProject.Tests.Common;
    using Microsoft.Extensions.Logging;
    using NSubstitute;
    using NUnit.Framework;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    [TestFixture]
    public class DrainReaperTests
    {
        [Test]
        public async Task ExecuteAsync_ActiveZero_RemovesAndLogs()
        {
            // Arrange
            var drain = new DrainController();
            var metrics = Substitute.For<IConnectionMetrics>();
            var registry = Substitute.For<IBackendRegistry>();
            var logger = Substitute.For<ILogger<DrainReaper>>();
            var sut = new DrainReaper(drain, metrics, registry, logger);

            var backend = new Uri("tcp://host:1");
            registry.Remove(backend).Returns(true);
            drain.BeginDrain(backend);

            metrics.Snapshot().Returns(new MetricsSnapshot(
                new[] { new BackendMetrics(backend.ToString(), 0, 0) }, 0, 0));

            // Act
            await sut.StartAsync(CancellationToken.None);
            await Task.Delay(1200);
            await sut.StopAsync(CancellationToken.None);

            // Assert
            registry.Received(1).Remove(backend);
            logger.AssertLogContains(LogLevel.Information, "Safely removed backend");
            Assert.That(drain.IsDraining(backend), Is.False);
        }

        [Test]
        public async Task ExecuteAsync_TimeoutHit_RemovesEvenIfActive()
        {
            // Arrange
            var drain = new DrainController();
            var metrics = Substitute.For<IConnectionMetrics>();
            var registry = Substitute.For<IBackendRegistry>();
            var logger = Substitute.For<ILogger<DrainReaper>>();
            var sut = new DrainReaper(drain, metrics, registry, logger);

            var backend = new Uri("tcp://host:2");
            registry.Remove(backend).Returns(true);
            drain.BeginDrain(backend, TimeSpan.FromMilliseconds(50));

            metrics.Snapshot().Returns(new MetricsSnapshot(
                new[] { new BackendMetrics(backend.ToString(), 5, 10) }, 5, 10));

            // Act
            await sut.StartAsync(CancellationToken.None);
            await Task.Delay(1200);
            await sut.StopAsync(CancellationToken.None);

            // Assert
            registry.Received(1).Remove(backend);
            logger.AssertLogContains(LogLevel.Information, "Safely removed backend");
            Assert.That(drain.IsDraining(backend), Is.False);
        }
    }
}
