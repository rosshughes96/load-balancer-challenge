using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace LoadBalancerProject.Backends
{
    public class BackendRegistry : IBackendRegistry
    {
        private readonly ConcurrentDictionary<string, Uri> _map = new();
        private ILogger<BackendRegistry> logger;

        public BackendRegistry(ILogger<BackendRegistry> logger)
        {
            this.logger = logger;
        }

        public bool Add(Uri backend) => _map.TryAdd(Norm(backend), backend);
        public bool Remove(Uri backend) => _map.TryRemove(Norm(backend), out _);
        public bool Contains(Uri backend) => _map.ContainsKey(Norm(backend));
        public IReadOnlyList<Uri> List() => _map.Values.OrderBy(u => u.ToString()).ToList();

        public void SetAll(IEnumerable<Uri> backends)
        {
            _map.Clear();
            foreach (var b in backends) Add(b);
        }

        private static string Norm(Uri u) => u.ToString().ToLowerInvariant();
    }
}