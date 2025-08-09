namespace LoadBalancerProject.LoadBalancing.Strategies
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    /// <summary>
    /// Selects backends in a strict round-robin order.
    /// </summary>
    public sealed class RoundRobinStrategy : IBackendSelectorStrategy
    {
        private int _next = -1; // first increment => index 0

        /// <inheritdoc/>
        public Uri SelectBackend(IReadOnlyList<Uri> backends)
        {
            if (backends == null || backends.Count == 0)
                throw new InvalidOperationException("No backends provided.");

            // Increment atomically to support multi-threaded access
            var i = Interlocked.Increment(ref _next);

            // Use modulo to wrap around, mask to avoid negatives if overflow occurs
            var idx = (i & int.MaxValue) % backends.Count;
            return backends[idx];
        }
    }
}
