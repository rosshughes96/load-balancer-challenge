using LoadBalancerProject.Configuration;
using LoadBalancerProject.LoadBalancing.Strategies;
using Microsoft.Extensions.Logging;

namespace LoadBalancerProject.Strategies
{
    public interface IStrategyProvider
    {
        IBackendSelectorStrategy Current { get; }
        void Refresh();
    }

    public class StrategyProvider : IStrategyProvider
    {
        private readonly IDynamicConfig _config;
        private readonly RoundRobinStrategy _roundRobin;
        private readonly LeastQueueStrategy _leastQueue;
        private IBackendSelectorStrategy _current;
        private readonly ILogger<StrategyProvider> _logger;

        public StrategyProvider(
            IDynamicConfig config,
            RoundRobinStrategy roundRobin,
            LeastQueueStrategy leastQueue,
            ILogger<StrategyProvider> logger)
        {
            _config = config;
            _roundRobin = roundRobin;
            _leastQueue = leastQueue;
            _logger = logger;

            _current = Resolve(_config.Strategy);
            _logger.LogInformation("StrategyProvider initialised with {Strategy}", _config.Strategy);
        }

        public IBackendSelectorStrategy Current => _current;

        public void Refresh()
        {
            _current = Resolve(_config.Strategy);
            _logger.LogInformation("Strategy changed to {Strategy}", _config.Strategy);
        }

        private IBackendSelectorStrategy Resolve(string name) =>
            name?.Equals("LeastQueue", StringComparison.OrdinalIgnoreCase) == true
                ? _leastQueue
                : _roundRobin; // default
    }
}