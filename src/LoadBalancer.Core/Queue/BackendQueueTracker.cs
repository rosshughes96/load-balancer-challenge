namespace LoadBalancerProject.Queue
{
    using System;
    using System.Collections.Concurrent;

    /// <summary>
    /// Thread-safe implementation of <see cref="IBackendQueueTracker"/> using an in-memory dictionary.
    /// </summary>
    public sealed class BackendQueueTracker : IBackendQueueTracker
    {
        private readonly ConcurrentDictionary<string, int> _map = new();

        /// <inheritdoc/>
        public void Increment(Uri backend)
        {
            if (backend == null) throw new ArgumentNullException(nameof(backend));

            // Add or increment the count for the backend
            _map.AddOrUpdate(backend.ToString(), 1, (_, v) => v + 1);
        }

        /// <inheritdoc/>
        public void Decrement(Uri backend)
        {
            if (backend == null) throw new ArgumentNullException(nameof(backend));

            // Decrement but never go below zero
            _map.AddOrUpdate(backend.ToString(), 0, (_, v) => Math.Max(0, v - 1));
        }

        /// <inheritdoc/>
        public int GetQueueLength(Uri backend)
        {
            if (backend == null) throw new ArgumentNullException(nameof(backend));

            return _map.TryGetValue(backend.ToString(), out var v) ? v : 0;
        }
    }
}
