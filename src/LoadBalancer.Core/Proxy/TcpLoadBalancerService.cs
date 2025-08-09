namespace LoadBalancerProject.Proxy
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using LoadBalancerProject.LoadBalancing;
    using LoadBalancerProject.Options;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Background TCP listener that accepts client connections on a configured port
    /// and forwards them to a selected healthy backend using the <see cref="IRequestForwarder"/>.
    /// </summary>
    public sealed class TcpLoadBalancerService : BackgroundService
    {
        private readonly ILogger<TcpLoadBalancerService> _logger;
        private readonly ILoadBalancer _lb;
        private readonly IRequestForwarder _forwarder;
        private readonly int _listenPort;

        /// <summary>
        /// Initializes a new instance of the <see cref="TcpLoadBalancerService"/> class.
        /// </summary>
        public TcpLoadBalancerService(
            ILogger<TcpLoadBalancerService> logger,
            IOptions<LoadBalancerOptions> opts,
            ILoadBalancer lb,
            IRequestForwarder forwarder)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _lb = lb ?? throw new ArgumentNullException(nameof(lb));
            _forwarder = forwarder ?? throw new ArgumentNullException(nameof(forwarder));
            _listenPort = opts?.Value?.ListenPort == 0 ? 6000 : opts!.Value.ListenPort;
        }

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var listener = new TcpListener(IPAddress.IPv6Any, _listenPort)
            {
                Server = { DualMode = true } // Accept IPv4 & IPv6
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
                // Expected during shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TcpLoadBalancerService main loop");
            }
            finally
            {
                try { listener.Stop(); } catch { /* Ignore */ }
            }
        }

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
                try { client.Close(); } catch { /* Ignore */ }
            }
        }
    }
}
