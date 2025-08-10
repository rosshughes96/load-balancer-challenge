namespace LoadBalancerProject.Backends
{
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Thread-safe in-memory registry of backends.
    /// </summary>
    public class BackendRegistry : IBackendRegistry
    {
        private readonly ConcurrentDictionary<string, Uri> _map = new();
        private readonly ILogger<BackendRegistry> _logger;

        /// <summary>
        /// Creates the registry.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public BackendRegistry(ILogger<BackendRegistry> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc/>
        public bool Add(Uri backend)
        {
            var key = Norm(backend);
            var added = _map.TryAdd(key, backend);

            if (added)
            {
                _logger.LogInformation("Added backend {Backend}", backend);
            }
            else
            {
                _logger.LogDebug("Backend {Backend} already exists", backend);
            }

            return added;
        }

        /// <inheritdoc/>
        public bool Remove(Uri backend)
        {
            var key = Norm(backend);
            var removed = _map.TryRemove(key, out _);

            if (removed)
            {
                _logger.LogInformation("Removed backend {Backend}", backend);
            }
            else
            {
                _logger.LogDebug("Backend {Backend} not found", backend);
            }

            return removed;
        }

        /// <inheritdoc/>
        public bool Contains(Uri backend)
        {
            var exists = _map.ContainsKey(Norm(backend));
            _logger.LogDebug("Contains check for {Backend}: {Exists}", backend, exists);
            return exists;
        }

        /// <inheritdoc/>
        public IReadOnlyList<Uri> List()
        {
            var list = _map.Values.OrderBy(u => u.ToString()).ToList();
            _logger.LogDebug("Listed {Count} backends", list.Count);
            return list;
        }

        /// <summary>
        /// Normalizes a backend address for use as a key.
        /// </summary>
        /// <param name="u">The backend address.</param>
        /// <returns>The normalized key.</returns>
        private static string Norm(Uri u) => u.ToString().ToLowerInvariant();
    }
}
