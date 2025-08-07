namespace LoadBalancerProject.Tests.Proxy
{
    using LoadBalancerProject.Metrics;
    using LoadBalancerProject.Options;
    using LoadBalancerProject.Proxy;
    using LoadBalancerProject.Queue;
    using LoadBalancerProject.Tests.Common;
    using Microsoft.Extensions.Logging;
    using NSubstitute;
    using NUnit.Framework;
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;

    [TestFixture]
    public class TcpRequestForwarderTests
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
        public async Task ForwardAsync_MaxConnectionsReached_LogsWarningAndClosesClientWithoutMetrics()
        {
            // Arrange
            var logger = Substitute.For<ILogger<TcpRequestForwarder>>();
            var queue = Substitute.For<IBackendQueueTracker>();
            var metrics = Substitute.For<IConnectionMetrics>();
            var opts = new TcpForwarderOptions
            {
                MaxConcurrentConnections = 0,
                IdleTimeoutSeconds = 1,
                MaxLifetimeSeconds = 10,
                BufferSize = 1024
            };
            var sut = new TcpRequestForwarder(logger, queue, opts, metrics);
            var client = new TcpClient();
            var backend = new Uri("tcp://127.0.0.1:12345");

            // Act
            await sut.ForwardAsync(backend, client);

            // Assert
            logger.AssertLogContains(LogLevel.Warning, "Max connections reached");
            queue.DidNotReceiveWithAnyArgs().Increment(default!);
            metrics.DidNotReceiveWithAnyArgs().OnConnectionStart(default!);
        }

        [Test]
        public async Task ForwardAsync_NoTraffic_IdleTimeoutTriggers_LogsAndMetricsUpdated()
        {
            // Arrange
            var logger = Substitute.For<ILogger<TcpRequestForwarder>>();
            var queue = Substitute.For<IBackendQueueTracker>();
            var metrics = Substitute.For<IConnectionMetrics>();
            var opts = new TcpForwarderOptions
            {
                MaxConcurrentConnections = 10,
                IdleTimeoutSeconds = 1,
                MaxLifetimeSeconds = 10,
                BufferSize = 1024
            };
            var sut = new TcpRequestForwarder(logger, queue, opts, metrics);

            int frontPort = GetFreePort();
            var frontListener = new TcpListener(IPAddress.Loopback, frontPort);
            frontListener.Start();
            var frontAcceptTask = frontListener.AcceptTcpClientAsync();

            var frontClient = new TcpClient();
            await frontClient.ConnectAsync(IPAddress.Loopback, frontPort);
            var acceptedClient = await frontAcceptTask;

            int backendPort = GetFreePort();
            var backendUri = new Uri($"tcp://127.0.0.1:{backendPort}");
            var backendListener = new TcpListener(IPAddress.Loopback, backendPort);
            backendListener.Start();
            var backendAcceptTask = backendListener.AcceptTcpClientAsync();

            // Act
            var forwardTask = sut.ForwardAsync(backendUri, acceptedClient);
            var backendConn = await backendAcceptTask;

            await forwardTask;

            // Assert
            logger.AssertLogContains(LogLevel.Information, "Connecting to backend");
            logger.AssertLogContains(LogLevel.Warning, "Idle timeout in c2b");
            logger.AssertLogContains(LogLevel.Information, "Relay finished");
            queue.Received(1).Increment(backendUri);
            queue.Received(1).Decrement(backendUri);
            metrics.Received(1).OnConnectionStart(backendUri);
            metrics.Received(1).OnConnectionEnd(backendUri);

            // Cleanup
            backendConn.Close();
            frontClient.Close();
            frontListener.Stop();
            backendListener.Stop();
        }

        [Test]
        public async Task ForwardAsync_BackendClosesFirst_LogsEofOnB2C_AndFinishes()
        {
            // Arrange
            var logger = Substitute.For<ILogger<TcpRequestForwarder>>();
            var queue = Substitute.For<IBackendQueueTracker>();
            var metrics = Substitute.For<IConnectionMetrics>();
            var opts = new TcpForwarderOptions
            {
                MaxConcurrentConnections = 10,
                IdleTimeoutSeconds = 10,
                MaxLifetimeSeconds = 10,
                BufferSize = 1024
            };
            var sut = new TcpRequestForwarder(logger, queue, opts, metrics);

            int frontPort = GetFreePort();
            var frontListener = new TcpListener(IPAddress.Loopback, frontPort);
            frontListener.Start();
            var frontAcceptTask = frontListener.AcceptTcpClientAsync();
            var frontClient = new TcpClient();
            await frontClient.ConnectAsync(IPAddress.Loopback, frontPort);
            var acceptedClient = await frontAcceptTask;

            int backendPort = GetFreePort();
            var backendUri = new Uri($"tcp://127.0.0.1:{backendPort}");
            var backendListener = new TcpListener(IPAddress.Loopback, backendPort);
            backendListener.Start();
            var backendAcceptTask = backendListener.AcceptTcpClientAsync();

            // Act
            var forwardTask = sut.ForwardAsync(backendUri, acceptedClient);
            var backendConn = await backendAcceptTask;

            backendConn.Close();

            await forwardTask;

            // Assert
            logger.AssertLogContains(LogLevel.Debug, "EOF on b2c");
            logger.AssertLogContains(LogLevel.Information, "Relay finished");
            queue.Received(1).Increment(backendUri);
            queue.Received(1).Decrement(backendUri);
            metrics.Received(1).OnConnectionStart(backendUri);
            metrics.Received(1).OnConnectionEnd(backendUri);

            // Cleanup
            frontClient.Close();
            frontListener.Stop();
            backendListener.Stop();
        }
    }
}
