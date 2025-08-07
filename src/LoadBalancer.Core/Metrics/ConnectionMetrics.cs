namespace LoadBalancerProject.Metrics
{
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Threading;

    /// <summary>
    /// Thread-safe in-memory implementation of <see cref="IConnectionMetrics"/>.
    /// </summary>
    public sealed class ConnectionMetrics : IConnectionMetrics
    {
        private class Counter
        {
            public int Active;
            public long Total;
        }

        private readonly ConcurrentDictionary<string, Counter> _map = new();
        private readonly ILogger<ConnectionMetrics> _logger;

        /// <summary>
        /// Creates a new metrics tracker.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public ConnectionMetrics(ILogger<ConnectionMetrics> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc/>
        public void OnConnectionStart(Uri backend)
        {
            var key = backend.ToString();
            var c = _map.GetOrAdd(key, _ => new Counter());
            Interlocked.Increment(ref c.Active);
            Interlocked.Increment(ref c.Total);

            _logger.LogDebug("Connection started to {Backend}. Active={Active}, Total={Total}", key, c.Active, c.Total);
        }

        /// <inheritdoc/>
        public void OnConnectionEnd(Uri backend)
        {
            var key = backend.ToString();
            if (_map.TryGetValue(key, out var c))
            {
                var newActive = Interlocked.Decrement(ref c.Active);

                if (newActive < 0)
                {
                    Interlocked.Exchange(ref c.Active, 0);
                    _logger.LogWarning("Active count went negative for {Backend}. Clamped to 0.", key);
                }

                _logger.LogDebug("Connection ended to {Backend}. Active={Active}", key, Math.Max(0, newActive));
            }
        }

        /// <inheritdoc/>
        public MetricsSnapshot Snapshot()
        {
            var list = _map
                .Select(kvp => new BackendMetrics(kvp.Key, kvp.Value.Active, kvp.Value.Total))
                .OrderBy(b => b.Backend)
                .ToList();

            var activeAll = list.Sum(b => b.Active);
            var totalAll = list.Sum(b => b.Total);

            _logger.LogDebug("Metrics snapshot created. ActiveAll={ActiveAll}, TotalAll={TotalAll}", activeAll, totalAll);

            return new MetricsSnapshot(list, activeAll, totalAll);
        }
    }
}
