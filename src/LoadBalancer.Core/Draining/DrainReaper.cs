namespace LoadBalancerProject.Draining
{
    using LoadBalancerProject.Backends;
    using LoadBalancerProject.Metrics;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Removes backends after safe removal once active connections are zero or timeout is reached.
    /// </summary>
    public sealed class DrainReaper : BackgroundService
    {
        private readonly DrainController _drain;
        private readonly IConnectionMetrics _metrics;
        private readonly IBackendRegistry _registry;
        private readonly ILogger<DrainReaper> _logger;

        /// <summary>
        /// Creates a new drain reaper.
        /// </summary>
        public DrainReaper(
            DrainController drain,
            IConnectionMetrics metrics,
            IBackendRegistry registry,
            ILogger<DrainReaper> logger)
        {
            _drain = drain;
            _metrics = metrics;
            _registry = registry;
            _logger = logger;
        }

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var snap = _metrics.Snapshot();
                    foreach (var kv in _drain.Snapshot())
                    {
                        var key = kv.Key;
                        var info = kv.Value;

                        var active = snap.Backends.FirstOrDefault(b => b.Backend == key)?.Active ?? 0;
                        var timeoutHit = info.Timeout.HasValue &&
                                         DateTimeOffset.UtcNow - info.Started >= info.Timeout.Value;

                        if (active <= 0 || timeoutHit)
                        {
                            if (_registry.Remove(new Uri(key)))
                            {
                                _logger.LogInformation(
                                    "Safely removed backend {Backend} (active={Active}, timeoutHit={TimeoutHit})",
                                    key, active, timeoutHit);
                            }

                            _drain.Clear(new Uri(key));
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Drain reaper loop error");
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                }
                catch
                {
                    // ignore cancellation
                }
            }
        }
    }
}
