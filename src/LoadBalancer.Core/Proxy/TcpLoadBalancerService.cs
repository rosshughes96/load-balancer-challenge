namespace LoadBalancerProject.Proxy
{
    using LoadBalancerProject.LoadBalancing;
    using LoadBalancerProject.Options;
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
    /// </summary>
    public sealed class TcpLoadBalancerService : BackgroundService
    {
        private readonly ILogger<TcpLoadBalancerService> _logger;
        private readonly ILoadBalancer _lb;
        private readonly IRequestForwarder _forwarder;
        private readonly int _listenPort;

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
        }

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var listener = new TcpListener(IPAddress.IPv6Any, _listenPort)
            {
                Server = { DualMode = true } // accept IPv4 & IPv6
            };

            listener.Start();
            _logger.LogInformation("TCP load balancer listening on port {Port}", _listenPort);

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var client = await listener.AcceptTcpClientAsync(stoppingToken);
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
        /// </summary>
        private async Task HandleClientAsync(TcpClient client, CancellationToken ct)
        {
            try
            {
                var backend = _lb.SelectBackend();
                _logger.LogInformation("Accepted client {Remote}, forwarding to {Backend}",
                    client.Client.RemoteEndPoint, backend);

                await _forwarder.ForwardAsync(backend, client);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling TCP client");
                try { client.Close(); } catch { }
            }
        }
    }
}
