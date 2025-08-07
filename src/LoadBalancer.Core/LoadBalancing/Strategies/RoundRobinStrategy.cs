namespace LoadBalancerProject.LoadBalancing.Strategies
{
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Threading;

    /// <summary>
    /// Selects backends in a round-robin order.
    /// </summary>
    public sealed class RoundRobinStrategy : IBackendSelectorStrategy
    {
        private int _next = -1; // first increment -> 0
        private readonly ILogger<RoundRobinStrategy> _logger;

        /// <summary>
        /// Creates a new round robin strategy.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public RoundRobinStrategy(ILogger<RoundRobinStrategy> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc/>
        public Uri SelectBackend(IReadOnlyList<Uri> backends)
        {
            if (backends == null || backends.Count == 0)
                throw new InvalidOperationException("No backends provided.");

            var i = Interlocked.Increment(ref _next);       // atomic across threads
            var idx = (i & int.MaxValue) % backends.Count;  // keep index positive
            var selected = backends[idx];

            _logger.LogDebug("Round robin selected backend {Backend} at index {Index}", selected, idx);
            return selected;
        }
    }
}
