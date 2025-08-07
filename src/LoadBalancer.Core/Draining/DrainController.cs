namespace LoadBalancerProject.Draining
{
    using System;
    using System.Collections.Concurrent;

    /// <summary>
    /// Tracks backends that are being safely removed.
    /// </summary>
    public sealed class DrainController : IDrainController
    {
        /// <summary>
        /// Holds drain start time and optional timeout.
        /// </summary>
        internal sealed record DrainInfo(DateTimeOffset Started, TimeSpan? Timeout);

        private readonly ConcurrentDictionary<string, DrainInfo> _map = new();

        /// <inheritdoc/>
        public void BeginDrain(Uri backend, TimeSpan? timeout = null)
        {
            _map[backend.ToString()] = new DrainInfo(DateTimeOffset.UtcNow, timeout);
        }

        /// <inheritdoc/>
        public bool IsDraining(Uri backend)
        {
            return _map.ContainsKey(backend.ToString());
        }

        /// <inheritdoc/>
        public void Clear(Uri backend)
        {
            _map.TryRemove(backend.ToString(), out _);
        }

        /// <summary>
        /// Gets a snapshot of the current drain map.
        /// </summary>
        /// <returns>The internal map of drain entries.</returns>
        internal ConcurrentDictionary<string, DrainInfo> Snapshot() => _map;
    }
}
