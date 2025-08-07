using System.Collections.Concurrent;

namespace LoadBalancerProject.Metrics
{
    public class ConnectionMetrics : IConnectionMetrics
    {
        private class Counter { public int Active; public long Total; }
        private readonly ConcurrentDictionary<string, Counter> _map = new();

        public void OnConnectionStart(Uri backend)
        {
            var key = backend.ToString();
            var c = _map.GetOrAdd(key, _ => new Counter());
            System.Threading.Interlocked.Increment(ref c.Active);
            System.Threading.Interlocked.Increment(ref c.Total);
        }

        public void OnConnectionEnd(Uri backend)
        {
            var key = backend.ToString();
            if (_map.TryGetValue(key, out var c))
            {
                System.Threading.Interlocked.Decrement(ref c.Active);
            }
        }

        public MetricsSnapshot Snapshot()
        {
            var list = _map.Select(kvp =>
                new BackendMetrics(kvp.Key, kvp.Value.Active, kvp.Value.Total)
            ).OrderBy(b => b.Backend).ToList();

            var activeAll = list.Sum(b => b.Active);
            var totalAll = list.Sum(b => b.Total);
            return new MetricsSnapshot(list, activeAll, totalAll);
        }
    }
}