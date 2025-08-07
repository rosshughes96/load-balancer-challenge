using System.Collections.Concurrent;

namespace LoadBalancerProject.Queue
{
    public class BackendQueueTracker : IBackendQueueTracker
    {
        private readonly ConcurrentDictionary<string, int> _map = new();

        public void Increment(Uri backend)
        {
            _map.AddOrUpdate(backend.ToString(), 1, (_, v) => v + 1);
        }

        public void Decrement(Uri backend)
        {
            _map.AddOrUpdate(backend.ToString(), 0, (_, v) => Math.Max(0, v - 1));
        }

        public int GetQueueLength(Uri backend)
        {
            return _map.TryGetValue(backend.ToString(), out var v) ? v : 0;
        }
    }
}