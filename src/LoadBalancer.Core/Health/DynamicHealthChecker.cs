using LoadBalancerProject.Backends;
using LoadBalancerProject.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Sockets;

namespace LoadBalancerProject.Health
{
    public class DynamicHealthChecker : BackgroundService, IHealthChecker
    {
        private readonly ILogger<DynamicHealthChecker> _logger;
        private readonly IBackendRegistry _registry;
        private readonly IDynamicConfig _config;
        private readonly List<Uri> _healthy = new();
        private readonly object _sync = new();

        public DynamicHealthChecker(ILogger<DynamicHealthChecker> logger, IBackendRegistry registry, IDynamicConfig config)
        {
            _logger = logger;
            _registry = registry;
            _config = config;
        }

        public IReadOnlyList<Uri> GetHealthyBackends()
        {
            // Arrange: take a snapshot under the lock, but return a *sorted* copy
            lock (_sync)
            {
                // stable, case-sensitive order to keep RR deterministic
                return _healthy
                    .OrderBy(u => u.ToString(), StringComparer.Ordinal)
                    .ToArray();
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await ProbeOnce(stoppingToken);
                var delay = Math.Max(1, _config.HealthCheckIntervalSeconds);
                await Task.Delay(TimeSpan.FromSeconds(delay), stoppingToken);
            }
        }

        private async Task ProbeOnce(CancellationToken ct)
        {
            var candidates = _registry.List();
            var healthy = new List<Uri>(candidates.Count);
            foreach (var uri in candidates)
            {
                try
                {
                    using var client = new TcpClient();
                    var task = client.ConnectAsync(uri.Host, uri.Port);
                    var completed = await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(1), ct));
                    if (completed == task && client.Connected)
                        healthy.Add(uri);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Health probe failed for {Backend}", uri);
                }
            }

            lock (_healthy)
            {
                _healthy.Clear();
                _healthy.AddRange(healthy);
            }

            _logger.LogInformation("Health: {Healthy}/{Total} backends healthy", healthy.Count, candidates.Count);
        }
    }
}