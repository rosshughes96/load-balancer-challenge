namespace LoadBalancerProject.LoadBalancing.Strategies
{
    using System;
    using System.Collections.Generic;
    using LoadBalancerProject.Queue;

    /// <summary>
    /// Selects the backend with the smallest current queue length.
    /// </summary>
    public sealed class LeastQueueStrategy : IBackendSelectorStrategy
    {
        private readonly IBackendQueueTracker _queue;

        /// <summary>
        /// Initializes a new instance of the <see cref="LeastQueueStrategy"/> class.
        /// </summary>
        /// <param name="queue">The backend queue tracker.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="queue"/> is null.</exception>
        public LeastQueueStrategy(IBackendQueueTracker queue)
        {
            _queue = queue ?? throw new ArgumentNullException(nameof(queue));
        }

        /// <inheritdoc/>
        public Uri SelectBackend(IReadOnlyList<Uri> healthyBackends)
        {
            if (healthyBackends == null || healthyBackends.Count == 0)
                throw new InvalidOperationException("No healthy backends available.");

            Uri best = healthyBackends[0];
            int bestQ = _queue.GetQueueLength(best);

            // Scan all backends to find the one with the smallest queue length
            for (int i = 1; i < healthyBackends.Count; i++)
            {
                var q = _queue.GetQueueLength(healthyBackends[i]);
                if (q < bestQ)
                {
                    best = healthyBackends[i];
                    bestQ = q;
                }
            }

            return best;
        }
    }
}
