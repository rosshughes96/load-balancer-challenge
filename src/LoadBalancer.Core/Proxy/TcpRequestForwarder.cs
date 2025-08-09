namespace LoadBalancerProject.Proxy
{
    using System;
    using System.Collections.Generic;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using LoadBalancerProject.Metrics;
    using LoadBalancerProject.Options;
    using LoadBalancerProject.Queue;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// TCP forwarder with connection limits, idle timeout, max lifetime,
    /// structured logging scopes, and metrics tracking.
    /// </summary>
    public sealed class TcpRequestForwarder : IRequestForwarder
    {
        private readonly ILogger<TcpRequestForwarder> _logger;
        private readonly IBackendQueueTracker _queueTracker;
        private readonly TcpForwarderOptions _options;
        private readonly IConnectionMetrics _metrics;

        private static int _activeConnections = 0;

        public TcpRequestForwarder(
            ILogger<TcpRequestForwarder> logger,
            IBackendQueueTracker queueTracker,
            TcpForwarderOptions options,
            IConnectionMetrics metrics)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _queueTracker = queueTracker ?? throw new ArgumentNullException(nameof(queueTracker));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
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

            // Enforce maximum concurrent connections
            if (Interlocked.Increment(ref _activeConnections) > _options.MaxConcurrentConnections)
            {
                _logger.LogWarning("Max connections reached ({Max}), rejecting client.", _options.MaxConcurrentConnections);
                Interlocked.Decrement(ref _activeConnections);
                try { client.Close(); } catch { /* ignore */ }
                return;
            }

            using var backendClient = new TcpClient();
            try
            {
                _logger.LogInformation("Connecting to backend {Backend}", backend);
                await backendClient.ConnectAsync(backend.Host, backend.Port);

                _queueTracker.Increment(backend);
                _metrics.OnConnectionStart(backend);

                using var clientStream = client.GetStream();
                using var backendStream = backendClient.GetStream();
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_options.MaxLifetimeSeconds));

                var c2b = RelayWithTimeoutAsync("ClientToBackend", clientStream, backendStream, cts.Token);
                var b2c = RelayWithTimeoutAsync("BackendToClient", backendStream, clientStream, cts.Token);

                await Task.WhenAny(c2b, b2c).ConfigureAwait(false);
                _logger.LogInformation("Relay finished for connection {ConnectionId}", connectionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error forwarding for connection {ConnectionId}", connectionId);
            }
            finally
            {
                _queueTracker.Decrement(backend);
                _metrics.OnConnectionEnd(backend);
                Interlocked.Decrement(ref _activeConnections);
                try { client.Close(); } catch { /* ignore */ }
            }
        }

        /// <summary>
        /// Relays data between two network streams with idle and lifetime timeouts enforced.
        /// </summary>
        private async Task RelayWithTimeoutAsync(string direction, NetworkStream from, NetworkStream to, CancellationToken lifetimeToken)
        {
            var buffer = new byte[_options.BufferSize];

            while (!lifetimeToken.IsCancellationRequested)
            {
                var readTask = from.ReadAsync(buffer, 0, buffer.Length, lifetimeToken);
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(_options.IdleTimeoutSeconds), lifetimeToken);

                var completed = await Task.WhenAny(readTask, timeoutTask).ConfigureAwait(false);

                if (completed != readTask)
                {
                    _logger.LogWarning("Idle timeout in {Direction}", direction);
                    break;
                }

                int bytesRead;
                try
                {
                    // Await the read task so cancellation/IO exceptions surface correctly
                    bytesRead = await readTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Lifetime/idle token canceled the read
                    break;
                }

                if (bytesRead <= 0)
                {
                    _logger.LogDebug("EOF on {Direction}", direction);
                    break;
                }

                await to.WriteAsync(buffer, 0, bytesRead, lifetimeToken).ConfigureAwait(false);
            }
        }
    }
}
