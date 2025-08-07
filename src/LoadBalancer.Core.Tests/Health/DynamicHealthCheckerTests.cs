namespace LoadBalancerProject.Tests.Health
{
    using LoadBalancerProject.Backends;
    using LoadBalancerProject.Configuration;
    using LoadBalancerProject.Draining;
    using LoadBalancerProject.Health;
    using LoadBalancerProject.Tests.Common;
    using Microsoft.Extensions.Logging;
    using NSubstitute;
    using NUnit.Framework;
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;

    [TestFixture]
    public class DynamicHealthCheckerTests
    {
        [Test]
        public async Task ExecuteAsync_WithHealthyAndDraining_OnlyHealthyIncludedAndLogged()
        {
            // Arrange
            var logger = Substitute.For<ILogger<DynamicHealthChecker>>();
            var portHealthy = GetFreePort();
            var healthyUri = new Uri($"tcp://127.0.0.1:{portHealthy}");
            var drainingUri = new Uri("tcp://127.0.0.1:65000");

            var registry = Substitute.For<IBackendRegistry>();
            registry.List().Returns(new[] { healthyUri, drainingUri });

            var config = Substitute.For<IDynamicConfig>();
            config.HealthCheckIntervalSeconds.Returns(1);

            var drain = Substitute.For<IDrainController>();
            drain.IsDraining(healthyUri).Returns(false);
            drain.IsDraining(drainingUri).Returns(true);

            var sut = new DynamicHealthChecker(logger, registry, config, drain);

            var listener = new TcpListener(IPAddress.Loopback, portHealthy);
            listener.Start();

            try
            {
                // Act
                await sut.StartAsync(CancellationToken.None);
                await Task.Delay(1200);
                var healthy = sut.GetHealthyBackends();
                await sut.StopAsync(CancellationToken.None);

                // Assert
                Assert.That(healthy, Is.EquivalentTo(new[] { healthyUri }));
                logger.AssertLogContains(LogLevel.Debug, "is healthy");
                logger.AssertLogContains(LogLevel.Information, "1/1 healthy");
            }
            finally
            {
                try { listener.Stop(); } catch { }
            }
        }

        [Test]
        public async Task ExecuteAsync_BackendTimesOut_LogsTimeoutAndNoHealthy()
        {
            // Arrange
            var logger = Substitute.For<ILogger<DynamicHealthChecker>>();
            var portNoListener = GetFreePort();
            var target = new Uri($"tcp://127.0.0.1:{portNoListener}");

            var registry = Substitute.For<IBackendRegistry>();
            registry.List().Returns(new[] { target });

            var config = Substitute.For<IDynamicConfig>();
            config.HealthCheckIntervalSeconds.Returns(1);

            var drain = Substitute.For<IDrainController>();
            drain.IsDraining(target).Returns(false);

            var sut = new DynamicHealthChecker(logger, registry, config, drain);

            // Act
            await sut.StartAsync(CancellationToken.None);
            await Task.Delay(1200);
            var healthy = sut.GetHealthyBackends();
            await sut.StopAsync(CancellationToken.None);

            // Assert
            Assert.That(healthy, Is.Empty);
            logger.AssertLogContains(LogLevel.Debug, "timed out");
            logger.AssertLogContains(LogLevel.Information, "0/1 healthy");
        }

        [Test]
        public async Task GetHealthyBackends_WhenMultipleHealthy_ReturnsSortedCopy()
        {
            // Arrange
            var logger = Substitute.For<ILogger<DynamicHealthChecker>>();
            var p1 = GetFreePort();
            var p2 = GetFreePort();
            var u1 = new Uri($"tcp://127.0.0.1:{p1}");
            var u2 = new Uri($"tcp://127.0.0.1:{p2}");

            var l1 = new TcpListener(IPAddress.Loopback, p1);
            var l2 = new TcpListener(IPAddress.Loopback, p2);
            l1.Start(); l2.Start();

            var registry = Substitute.For<IBackendRegistry>();
            registry.List().Returns(new[] { u2, u1 });

            var config = Substitute.For<IDynamicConfig>();
            config.HealthCheckIntervalSeconds.Returns(1);

            var drain = Substitute.For<IDrainController>();
            drain.IsDraining(Arg.Any<Uri>()).Returns(false);

            var sut = new DynamicHealthChecker(logger, registry, config, drain);

            try
            {
                // Act
                await sut.StartAsync(CancellationToken.None);
                await Task.Delay(1200);
                var healthy = sut.GetHealthyBackends();
                await sut.StopAsync(CancellationToken.None);

                // Assert
                Assert.That(healthy.Select(u => u.ToString()), Is.EqualTo(new[] { u1.ToString(), u2.ToString() }.OrderBy(s => s, StringComparer.Ordinal)));
                logger.AssertLogContains(LogLevel.Information, "2/2 healthy");
            }
            finally
            {
                try { l1.Stop(); } catch { }
                try { l2.Stop(); } catch { }
            }
        }

        private static int GetFreePort()
        {
            var l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }
    }
}
