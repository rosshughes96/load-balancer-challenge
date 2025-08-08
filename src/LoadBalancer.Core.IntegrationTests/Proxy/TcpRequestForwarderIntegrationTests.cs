using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using LoadBalancerProject.Metrics;
using LoadBalancerProject.Options;
using LoadBalancerProject.Proxy;
using LoadBalancerProject.Queue;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace LoadBalancerProject.IntegrationTests.Proxy;

[Category("Integration")]
[TestFixture]
public class TcpRequestForwarderIntegrationTests
{
    [Test]
    public async Task ForwardAsync_When_BackendEchoes_Should_RelayRequestAndResponse()
    {
        // Arrange
        var backendPort = GetFreePort();
        using var backendListener = new TcpListener(IPAddress.Loopback, backendPort);
        backendListener.Start();

        // simple echo backend
        var backendTask = Task.Run(async () =>
        {
            using var serverClient = await backendListener.AcceptTcpClientAsync();
            using var stream = serverClient.GetStream();
            var buf = new byte[1024];
            var n = await stream.ReadAsync(buf, 0, buf.Length);
            await stream.WriteAsync(buf, 0, n);
        });

        var frontListener = new TcpListener(IPAddress.Loopback, 0);
        frontListener.Start();
        var frontPort = ((IPEndPoint)frontListener.LocalEndpoint).Port;

        var logger = Substitute.For<ILogger<TcpRequestForwarder>>();
        var tracker = Substitute.For<IBackendQueueTracker>();
        var metrics = Substitute.For<IConnectionMetrics>();
        var options = new TcpForwarderOptions { IdleTimeoutSeconds = 5, MaxLifetimeSeconds = 10, BufferSize = 4096, MaxConcurrentConnections = 100 };

        var sut = new TcpRequestForwarder(logger, tracker, options, metrics);

        var backendUri = new Uri($"tcp://127.0.0.1:{backendPort}");

        // Accept the "incoming" connection that will be forwarded
        var acceptTask = frontListener.AcceptTcpClientAsync();

        // Act
        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, frontPort);
        var clientStream = client.GetStream();
        var payload = Encoding.UTF8.GetBytes("hello");
        await clientStream.WriteAsync(payload, 0, payload.Length);

        using var accepted = await acceptTask;
        var forwardTask = sut.ForwardAsync(backendUri, accepted);

        var buf2 = new byte[1024];
        var n2 = await clientStream.ReadAsync(buf2, 0, buf2.Length);
        var echo = Encoding.UTF8.GetString(buf2, 0, n2);

        // Assert
        Assert.That(echo, Is.EqualTo("hello"));

        // Cleanup
        client.Close();
        frontListener.Stop();
        backendListener.Stop();
        await Task.WhenAll(backendTask, forwardTask);
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