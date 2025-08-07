namespace LoadBalancerProject.Health
{
    using LoadBalancerProject.Backends;
    using LoadBalancerProject.Configuration;
    using LoadBalancerProject.Draining;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Probes backends and tracks which are healthy.
    /// </summary>
    public sealed class DynamicHealthChecker : BackgroundService, IHealthChecker
    {
        private readonly ILogger<DynamicHealthChecker> _logger;
        private readonly IBackendRegistry _registry;
        private readonly IDynamicConfig _config;
        private readonly List<Uri> _healthy = new();
        private readonly object _sync = new();
        private readonly IDrainController _drain;

        /// <summary>
        /// Creates a new health checker.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="registry">The backend registry.</param>
        /// <param name="config">The dynamic configuration.</param>
        /// <param name="drain">The drain controller.</param>
        public DynamicHealthChecker(
            ILogger<DynamicHealthChecker> logger,
            IBackendRegistry registry,
            IDynamicConfig config,
            IDrainController drain)
        {
            _logger = logger;
            _registry = registry;
            _config = config;
            _drain = drain;
        }

        /// <inheritdoc/>
        public IReadOnlyList<Uri> GetHealthyBackends()
        {
            lock (_sync)
            {
                return _healthy
                    .OrderBy(u => u.ToString(), StringComparer.Ordinal)
                    .ToArray();
            }
        }

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Health checker started");

            while (!stoppingToken.IsCancellationRequested)
            {
                await ProbeOnce(stoppingToken);
                var delay = Math.Max(1, _config.HealthCheckIntervalSeconds);
                await Task.Delay(TimeSpan.FromSeconds(delay), stoppingToken);
            }

            _logger.LogInformation("Health checker stopped");
        }

        /// <summary>
        /// Probes all registered backends and updates the healthy list.
        /// </summary>
        private async Task ProbeOnce(CancellationToken ct)
        {
            var candidates = _registry
                .List()
                .Where(u => !_drain.IsDraining(u))
                .ToList();

            var healthy = new List<Uri>(candidates.Count);

            foreach (var uri in candidates)
            {
                try
                {
                    using var client = new TcpClient();
                    var connectTask = client.ConnectAsync(uri.Host, uri.Port);
                    var completed = await Task.WhenAny(connectTask, Task.Delay(TimeSpan.FromSeconds(1), ct));

                    if (completed == connectTask && client.Connected)
                    {
                        healthy.Add(uri);
                        _logger.LogDebug("Backend {Backend} is healthy", uri);
                    }
                    else
                    {
                        _logger.LogDebug("Backend {Backend} timed out", uri);
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

            _logger.LogInformation("Health check: {Healthy}/{Total} healthy", healthy.Count, candidates.Count);
        }
    }
}
