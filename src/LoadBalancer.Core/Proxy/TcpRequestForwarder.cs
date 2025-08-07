namespace LoadBalancerProject.Proxy
{
    using LoadBalancerProject.Metrics;
    using LoadBalancerProject.Options;
    using LoadBalancerProject.Queue;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// TCP forwarder with idle timeout, max lifetime, connection cap,
    /// structured logging scopes, and metrics hooks.
    /// </summary>
    public sealed class TcpRequestForwarder : IRequestForwarder
    {
        private readonly ILogger<TcpRequestForwarder> _logger;
        private readonly IBackendQueueTracker _queueTracker;
        private readonly TcpForwarderOptions _options;
        private readonly IConnectionMetrics _metrics;

        private static int _activeConnections;

        /// <summary>
        /// Creates a new TCP request forwarder.
        /// </summary>
        public TcpRequestForwarder(
            ILogger<TcpRequestForwarder> logger,
            IBackendQueueTracker queueTracker,
            TcpForwarderOptions options,
            IConnectionMetrics metrics)
        {
            _logger = logger;
            _queueTracker = queueTracker;
            _options = options;
            _metrics = metrics;
        }

        /// <inheritdoc/>
        public async Task ForwardAsync(Uri backend, TcpClient client)
        {
            var connectionId = Guid.NewGuid().ToString("N");

            using var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["Backend"] = backend.ToString(),
                ["ConnectionId"] = connectionId
            });

            if (Interlocked.Increment(ref _activeConnections) > _options.MaxConcurrentConnections)
            {
                _logger.LogWarning("Max connections reached ({Max}), rejecting new client", _options.MaxConcurrentConnections);
                Interlocked.Decrement(ref _activeConnections);
                try { client.Close(); } catch { }
                return;
            }

            using var backendClient = new TcpClient();
            try
            {
                _logger.LogInformation("Connecting to backend");
                await backendClient.ConnectAsync(backend.Host, backend.Port);

                _queueTracker.Increment(backend);
                _metrics.OnConnectionStart(backend);

                using var clientStream = client.GetStream();
                using var backendStream = backendClient.GetStream();

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_options.MaxLifetimeSeconds));

                var c2b = RelayWithTimeoutAsync("c2b", clientStream, backendStream, cts.Token);
                var b2c = RelayWithTimeoutAsync("b2c", backendStream, clientStream, cts.Token);

                await Task.WhenAny(c2b, b2c);
                _logger.LogInformation("Relay finished");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error forwarding");
            }
            finally
            {
                _queueTracker.Decrement(backend);
                _metrics.OnConnectionEnd(backend);
                Interlocked.Decrement(ref _activeConnections);
                try { client.Close(); } catch { }
            }
        }

        /// <summary>
        /// Relays data between two streams with idle timeout.
        /// </summary>
        private async Task RelayWithTimeoutAsync(string dir, NetworkStream from, NetworkStream to, CancellationToken lifetimeToken)
        {
            var buffer = new byte[_options.BufferSize];

            while (!lifetimeToken.IsCancellationRequested)
            {
                var readTask = from.ReadAsync(buffer, 0, buffer.Length, lifetimeToken);
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(_options.IdleTimeoutSeconds), lifetimeToken);

                var completed = await Task.WhenAny(readTask, timeoutTask).ConfigureAwait(false);

                if (completed != readTask)
                {
                    _logger.LogWarning("Idle timeout in {Dir}", dir);
                    break;
                }

                var n = readTask.Result;
                if (n <= 0)
                {
                    _logger.LogDebug("EOF on {Dir}", dir);
                    break;
                }

                await to.WriteAsync(buffer, 0, n, lifetimeToken).ConfigureAwait(false);
            }
        }
    }
}
