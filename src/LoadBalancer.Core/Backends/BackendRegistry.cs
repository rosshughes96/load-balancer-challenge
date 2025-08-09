namespace LoadBalancerProject.Backends
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Thread-safe in-memory implementation of <see cref="IBackendRegistry"/>.
    /// </summary>
    /// <remarks>
    /// Keys are normalized via <see cref="Uri.ToString"/> lower-cased, making the registry
    /// case-insensitive with respect to the string form of the URI.
    /// The <see cref="List"/> method returns a deterministic, lexicographically sorted snapshot,
    /// which keeps strategies like Round Robin predictable.
    /// </remarks>
    public sealed class BackendRegistry : IBackendRegistry
    {
        private readonly ConcurrentDictionary<string, Uri> _map = new();
        private readonly ILogger<BackendRegistry> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="BackendRegistry"/> class.
        /// </summary>
        /// <param name="logger">Logger for administrative operations.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is null.</exception>
        public BackendRegistry(ILogger<BackendRegistry> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public bool Add(Uri backend)
        {
            if (backend is null) throw new ArgumentNullException(nameof(backend));

            var key = Normalize(backend);
            var added = _map.TryAdd(key, backend);

            if (added)
            {
                _logger.LogInformation("Backend added: {Backend}", backend);
            }

            return added;
        }

        /// <inheritdoc />
        public bool Remove(Uri backend)
        {
            if (backend is null) throw new ArgumentNullException(nameof(backend));

            var key = Normalize(backend);
            var removed = _map.TryRemove(key, out _);

            if (removed)
            {
                _logger.LogInformation("Backend removed: {Backend}", backend);
            }

            return removed;
        }

        /// <inheritdoc />
        public bool Contains(Uri backend)
        {
            if (backend is null) throw new ArgumentNullException(nameof(backend));
            return _map.ContainsKey(Normalize(backend));
        }

        /// <inheritdoc />
        public IReadOnlyList<Uri> List()
        {
            // Return a stable, sorted snapshot so RR and other strategies behave deterministically.
            return _map.Values
                .OrderBy(u => u.ToString(), StringComparer.Ordinal)
                .ToList();
        }

        /// <inheritdoc />
        public void SetAll(IEnumerable<Uri> backends)
        {
            if (backends is null) throw new ArgumentNullException(nameof(backends));

            // Note: This is not atomic for concurrent readers. For the current use case this is acceptable.
            // If atomic replacement becomes a requirement, switch to a copy-on-write snapshot for readers.
            _map.Clear();

            int count = 0;
            foreach (var b in backends)
            {
                if (b is null) continue;
                if (_map.TryAdd(Normalize(b), b))
                {
                    count++;
                }
            }

            _logger.LogInformation("Backend registry replaced; total backends: {Count}", count);
        }

        /// <summary>
        /// Normalizes a URI to a case-insensitive string key.
        /// </summary>
        private static string Normalize(Uri uri) => uri.ToString().ToLowerInvariant();
    }
}
