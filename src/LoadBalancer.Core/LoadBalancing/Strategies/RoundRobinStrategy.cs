namespace LoadBalancerProject.LoadBalancing.Strategies
{
    using System.Threading;

    public sealed class RoundRobinStrategy : IBackendSelectorStrategy
    {
        private int _next = -1; // first increment -> 0

        public Uri SelectBackend(IReadOnlyList<Uri> backends)
        {
            if (backends == null || backends.Count == 0)
                throw new InvalidOperationException("No backends provided.");

            var i = Interlocked.Increment(ref _next);       // atomic across threads
            var idx = (i & int.MaxValue) % backends.Count;  // no negatives even if overflow years later
            return backends[idx];
        }
    }

}