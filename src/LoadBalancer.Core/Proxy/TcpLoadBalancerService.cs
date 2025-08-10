namespace LoadBalancerProject.Proxy
{
    using LoadBalancerProject.Diagnostics.Outage;
    using LoadBalancerProject.LoadBalancing;
    using LoadBalancerProject.Options;
    using LoadBalancerProject.Proxy.Refusal;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// TCP listener service that accepts clients and forwards them
    /// to a healthy backend chosen by the load balancer.
    /// When no healthy backends exist, connections are refused immediately and
    /// the service logs a single concise warning on outage entry and an info on recovery.
    /// </summary>
    public sealed class TcpLoadBalancerService : BackgroundService
    {
        private readonly ILogger<TcpLoadBalancerService> _logger;
        private readonly ILoadBalancer _lb;
        private readonly IRequestForwarder _forwarder;
        private readonly int _listenPort;
        private readonly OutageGate _outageGate;
        private readonly RefusalMode _refusalMode = RefusalMode.TcpReset;
        private readonly TimeSpan _acceptBackoffOnOutage = TimeSpan.FromMilliseconds(5);

        /// <summary>
        /// Creates the TCP load balancer service.
        /// </summary>
        public TcpLoadBalancerService(
            ILogger<TcpLoadBalancerService> logger,
            IOptions<LoadBalancerOptions> opts,
            ILoadBalancer lb,
            IRequestForwarder forwarder)
        {
            _logger = logger;
            _lb = lb;
            _forwarder = forwarder;
            _listenPort = opts.Value.ListenPort == 0 ? 6000 : opts.Value.ListenPort;
            _outageGate = new OutageGate(logger);
        }

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var listener = new TcpListener(IPAddress.IPv6Any, _listenPort)
            {
                Server = { DualMode = true }
            };

            listener.Start();
            _logger.LogInformation("TCP load balancer listening on port {Port}", _listenPort);

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    TcpClient client;

                    try
                    {
                        client = await listener.AcceptTcpClientAsync(stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        // Normal on shutdown.
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Accept failed.");
                        await Task.Delay(TimeSpan.FromMilliseconds(50), stoppingToken);
                        continue;
                    }

                    _ = HandleClientAsync(client, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // normal on shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TcpLoadBalancerService main loop error");
            }
            finally
            {
                try { listener.Stop(); } catch { }
            }
        }

        /// <summary>
        /// Handles a single client connection by forwarding it to a backend.
        /// If no healthy backend is available, refuses the connection immediately (RST or FIN)
        /// and applies a small backoff to avoid hot loops during a total outage.
        /// </summary>
        private async Task HandleClientAsync(TcpClient client, CancellationToken ct)
        {
            try
            {
                var backend = _lb.SelectBackend();

                _outageGate.OnRecovered();

                _logger.LogInformation("Accepted client {Remote}, forwarding to {Backend}",
                    client.Client.RemoteEndPoint, backend);

                await _forwarder.ForwardAsync(backend, client);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogDebug(ex, "Backend selection failed; refusing client {Remote}.",
                    SafeRemote(client));

                _outageGate.OnRefusal();

                var tcpRefuser = new TcpRefuser();
                tcpRefuser.Refuse(client, _refusalMode);

                try { await Task.Delay(_acceptBackoffOnOutage, ct); } catch { /* ignore */ }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling TCP client {Remote}", SafeRemote(client));
                try { client.Close(); } catch { }
            }
        }

        /// <summary>
        /// Safely formats a remote endpoint for logging, even if the client socket has already closed.
        /// </summary>
        private static object? SafeRemote(TcpClient client)
        {
            try { return client.Client?.RemoteEndPoint; } catch { return null; }
        }
    }
}
