namespace LoadBalancerProject.LoadBalancing.Strategies
{
    using LoadBalancerProject.Queue;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Selects the backend with the smallest queue length.
    /// </summary>
    public sealed class LeastQueueStrategy : IBackendSelectorStrategy
    {
        private readonly IBackendQueueTracker _queue;
        private readonly ILogger<LeastQueueStrategy> _logger;

        /// <summary>
        /// Creates a new instance of the strategy.
        /// </summary>
        /// <param name="queue">The queue tracker.</param>
        /// <param name="logger">The logger.</param>
        public LeastQueueStrategy(IBackendQueueTracker queue, ILogger<LeastQueueStrategy> logger)
        {
            _queue = queue;
            _logger = logger;
        }

        /// <inheritdoc/>
        public Uri SelectBackend(IReadOnlyList<Uri> healthyBackends)
        {
            if (healthyBackends == null || healthyBackends.Count == 0)
                throw new InvalidOperationException("No healthy backends.");

            Uri best = healthyBackends[0];
            int bestQ = _queue.GetQueueLength(best);

            for (int i = 1; i < healthyBackends.Count; i++)
            {
                var candidate = healthyBackends[i];
                var q = _queue.GetQueueLength(candidate);
                if (q < bestQ)
                {
                    best = candidate;
                    bestQ = q;
                }
            }

            _logger.LogDebug("Selected backend {Backend} with queue length {QueueLength}", best, bestQ);
            return best;
        }
    }
}
