using LoadBalancerProject.Queue;

namespace LoadBalancerProject.LoadBalancing.Strategies
{
    public class LeastQueueStrategy : IBackendSelectorStrategy
    {
        private readonly IBackendQueueTracker _queue;
        public LeastQueueStrategy(IBackendQueueTracker queue) { _queue = queue; }

        public Uri SelectBackend(IReadOnlyList<Uri> healthyBackends)
        {
            if (healthyBackends.Count == 0) throw new InvalidOperationException("No healthy backends.");
            Uri best = healthyBackends[0];
            int bestQ = _queue.GetQueueLength(best);
            for (int i = 1; i < healthyBackends.Count; i++)
            {
                var q = _queue.GetQueueLength(healthyBackends[i]);
                if (q < bestQ) { best = healthyBackends[i]; bestQ = q; }
            }
            return best;
        }
    }
}