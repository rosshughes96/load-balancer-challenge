namespace LoadBalancerProject.Configuration
{
    public class InMemoryDynamicConfig : IDynamicConfig
    {
        private string _strategy;
        private int _interval;

        public InMemoryDynamicConfig(string initialStrategy, int initialInterval)
        {
            _strategy = initialStrategy;
            _interval = initialInterval;
        }

        public string Strategy
        {
            get => Volatile.Read(ref _strategy);
            set => Interlocked.Exchange(ref _strategy, value);
        }

        public int HealthCheckIntervalSeconds
        {
            get => Volatile.Read(ref _interval);
            set => Interlocked.Exchange(ref _interval, value);
        }
    }
}