namespace LoadBalancerProject.Tests.Proxy
{
    using LoadBalancerProject.LoadBalancing;
    using LoadBalancerProject.Options;
    using LoadBalancerProject.Proxy;
    using LoadBalancerProject.Tests.Common;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using NSubstitute;
    using NUnit.Framework;
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;

    [TestFixture]
    public class TcpLoadBalancerServiceTests
    {
        private static int GetFreePort()
        {
            var l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            var port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }

        [Test]
        public async Task ExecuteAsync_OnClientConnection_CallsForwarderWithSelectedBackend_AndLogsListening()
        {
            // Arrange
            int port = GetFreePort();
            var logger = Substitute.For<ILogger<TcpLoadBalancerService>>();
            var lb = Substitute.For<ILoadBalancer>();
            var backend = new Uri("tcp://127.0.0.1:65000");
            lb.SelectBackend().Returns(backend);
            var fwd = Substitute.For<IRequestForwarder>();
            fwd.ForwardAsync(backend, Arg.Any<TcpClient>()).Returns(Task.CompletedTask);

            var opts = Options.Create(new LoadBalancerOptions
            {
                ListenPort = port
            });

            var sut = new TcpLoadBalancerService(logger, opts, lb, fwd);

            // Act
            await sut.StartAsync(CancellationToken.None);

            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, port);

            await Task.Delay(200);

            // Assert
            await fwd.Received(1).ForwardAsync(backend, Arg.Any<TcpClient>());
            logger.AssertLogContains(LogLevel.Information, "listening on port");

            // Cleanup
            await sut.StopAsync(CancellationToken.None);
        }

        [Test]
        public async Task HandleClientAsync_WhenForwarderThrows_LogsErrorAndContinues()
        {
            // Arrange
            int port = GetFreePort();
            var logger = Substitute.For<ILogger<TcpLoadBalancerService>>();
            var lb = Substitute.For<ILoadBalancer>();
            var backend = new Uri("tcp://127.0.0.1:65001");
            lb.SelectBackend().Returns(backend);

            var fwd = Substitute.For<IRequestForwarder>();
            fwd.ForwardAsync(backend, Arg.Any<TcpClient>()).Returns(ci => throw new InvalidOperationException("boom"));

            var opts = Options.Create(new LoadBalancerOptions { ListenPort = port });
            var sut = new TcpLoadBalancerService(logger, opts, lb, fwd);

            // Act
            await sut.StartAsync(CancellationToken.None);
            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, port);
            await Task.Delay(200);

            // Assert
            logger.AssertLogContains(LogLevel.Error, "Error handling TCP client");

            // Cleanup
            await sut.StopAsync(CancellationToken.None);
        }
    }
}
