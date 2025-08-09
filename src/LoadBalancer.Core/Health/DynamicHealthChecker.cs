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
    public sealed class DynamicHealthChecker : BackgroundService, IHealthChecker
    {
        private const int ProbeTimeoutSeconds = 1;

        private readonly ILogger<DynamicHealthChecker> _logger;
        private readonly IBackendRegistry _registry;
        private readonly IDynamicConfig _config;

        private readonly List<Uri> _healthy = new();
        private readonly object _sync = new();

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
                // Return a stable, sorted snapshot for deterministic strategy behavior.
                return _healthy
                    .OrderBy(u => u.ToString(), StringComparer.Ordinal)
                    .ToArray();
            }
        }

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await ProbeOnce(stoppingToken);
                var delaySeconds = Math.Max(1, _config.HealthCheckIntervalSeconds);
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);
            }
        }

        private async Task ProbeOnce(CancellationToken ct)
        {
            var candidates = _registry.List();
            var timeout = TimeSpan.FromSeconds(ProbeTimeoutSeconds);

            // Probe all candidates in parallel so a slow/down host doesn't serialize the loop.
            var tasks = candidates.Select(async uri =>
            {
                try
                {
                    using var client = new TcpClient();
                    var connect = client.ConnectAsync(uri.Host, uri.Port);
                    var done = await Task.WhenAny(connect, Task.Delay(timeout, ct)).ConfigureAwait(false);
                    var ok = done == connect && client.Connected;
                    return (uri, ok);
                }
                catch
                {
                    return (uri, ok: false);
                }
            }).ToArray();

            var results = await Task.WhenAll(tasks).ConfigureAwait(false);
            var healthyList = results.Where(x => x.ok).Select(x => x.uri).ToList();

            lock (_sync)
            {
                _healthy.Clear();
                _healthy.AddRange(healthyList);
            }

            _logger.LogInformation("Health: {Healthy}/{Total} backends healthy", healthyList.Count, candidates.Count);
        }
    }
}
