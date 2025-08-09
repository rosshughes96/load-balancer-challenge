namespace LoadBalancerProject.Metrics
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Threading;

    /// <summary>
    /// Thread-safe in-memory implementation of <see cref="IConnectionMetrics"/>.
    /// </summary>
    /// <remarks>
    /// Tracks both active connections and total connections since startup for each backend.
    /// </remarks>
    public sealed class ConnectionMetrics : IConnectionMetrics
    {
        /// <summary>
        /// Holds active and total connection counters for a backend.
        /// </summary>
        private sealed class Counter
        {
            public int Active;
            public long Total;
        }

        private readonly ConcurrentDictionary<string, Counter> _map = new();

        /// <inheritdoc />
        public void OnConnectionStart(Uri backend)
        {
            if (backend is null) throw new ArgumentNullException(nameof(backend));

            var key = backend.ToString();
            var counter = _map.GetOrAdd(key, _ => new Counter());

            Interlocked.Increment(ref counter.Active);
            Interlocked.Increment(ref counter.Total);
        }

        /// <inheritdoc />
        public void OnConnectionEnd(Uri backend)
        {
            if (backend is null) throw new ArgumentNullException(nameof(backend));

            var key = backend.ToString();
            if (_map.TryGetValue(key, out var counter))
            {
                Interlocked.Decrement(ref counter.Active);
            }
        }

        /// <inheritdoc />
        public MetricsSnapshot Snapshot()
        {
            var list = _map
                .Select(kvp => new BackendMetrics(kvp.Key, kvp.Value.Active, kvp.Value.Total))
                .OrderBy(b => b.Backend, StringComparer.Ordinal)
                .ToList();

            var activeAll = list.Sum(b => b.Active);
            var totalAll = list.Sum(b => b.Total);

            return new MetricsSnapshot(list, activeAll, totalAll);
        }
    }
}
