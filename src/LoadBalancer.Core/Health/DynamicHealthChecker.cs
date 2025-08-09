namespace LoadBalancerProject.Health
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using LoadBalancerProject.Backends;
    using LoadBalancerProject.Configuration;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Periodically probes all registered backends to determine their health status.
    /// </summary>
    /// <remarks>
    /// Uses TCP connect attempts to verify reachability. Healthy backends are stored in a thread-safe
    /// list and returned in a sorted order to keep strategies deterministic.
    /// </remarks>
    public sealed class DynamicHealthChecker : BackgroundService, IHealthChecker
    {
        private const int ProbeTimeoutSeconds = 1;

        private readonly ILogger<DynamicHealthChecker> _logger;
        private readonly IBackendRegistry _registry;
        private readonly IDynamicConfig _config;

        private readonly List<Uri> _healthy = new();
        private readonly object _sync = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicHealthChecker"/> class.
        /// </summary>
        /// <param name="logger">Logger for health probe results.</param>
        /// <param name="registry">Registry of configured backends.</param>
        /// <param name="config">Dynamic configuration source.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when any dependency is null.
        /// </exception>
        public DynamicHealthChecker(
            ILogger<DynamicHealthChecker> logger,
            IBackendRegistry registry,
            IDynamicConfig config)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <inheritdoc />
        public IReadOnlyList<Uri> GetHealthyBackends()
        {
            lock (_sync)
            {
                // Return a stable, sorted snapshot for deterministic strategy behavior
                return _healthy
                    .OrderBy(u => u.ToString(), StringComparer.Ordinal)
                    .ToArray();
            }
        }

        /// <summary>
        /// Executes the background health check loop until cancellation is requested.
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await ProbeOnce(stoppingToken);

                var delaySeconds = Math.Max(1, _config.HealthCheckIntervalSeconds);
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);
            }
        }

        /// <summary>
        /// Performs a single round of health probes against all registered backends.
        /// </summary>
        private async Task ProbeOnce(CancellationToken ct)
        {
            var candidates = _registry.List();
            var healthy = new List<Uri>(candidates.Count);

            foreach (var uri in candidates)
            {
                try
                {
                    using var client = new TcpClient();
                    var connectTask = client.ConnectAsync(uri.Host, uri.Port);
                    var completed = await Task.WhenAny(connectTask, Task.Delay(TimeSpan.FromSeconds(ProbeTimeoutSeconds), ct));

                    if (completed == connectTask && client.Connected)
                    {
                        healthy.Add(uri);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Health probe failed for {Backend}", uri);
                }
            }

            lock (_sync)
            {
                _healthy.Clear();
                _healthy.AddRange(healthy);
            }

            _logger.LogInformation("Health: {Healthy}/{Total} backends healthy", healthy.Count, candidates.Count);
        }
    }
}
