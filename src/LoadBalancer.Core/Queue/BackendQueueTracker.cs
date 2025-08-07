namespace LoadBalancerProject.Queue
{
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Concurrent;

    /// <summary>
    /// Thread-safe in-memory implementation of <see cref="IBackendQueueTracker"/>.
    /// </summary>
    public sealed class BackendQueueTracker : IBackendQueueTracker
    {
        private readonly ConcurrentDictionary<string, int> _map = new();
        private readonly ILogger<BackendQueueTracker> _logger;

        /// <summary>
        /// Creates a new backend queue tracker.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public BackendQueueTracker(ILogger<BackendQueueTracker> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc/>
        public void Increment(Uri backend)
        {
            var newValue = _map.AddOrUpdate(backend.ToString(), 1, (_, v) => v + 1);
            _logger.LogDebug("Incremented queue for {Backend} to {Value}", backend, newValue);
        }

        /// <inheritdoc/>
        public void Decrement(Uri backend)
        {
            var newValue = _map.AddOrUpdate(backend.ToString(), 0, (_, v) => Math.Max(0, v - 1));
            _logger.LogDebug("Decremented queue for {Backend} to {Value}", backend, newValue);
        }

        /// <inheritdoc/>
        public int GetQueueLength(Uri backend)
        {
            return _map.TryGetValue(backend.ToString(), out var value) ? value : 0;
        }
    }
}
