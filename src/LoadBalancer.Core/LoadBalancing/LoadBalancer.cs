using LoadBalancerProject.Health;
using LoadBalancerProject.Strategies;
using Microsoft.Extensions.Logging;

namespace LoadBalancerProject.LoadBalancing
{
    public class LoadBalancer : ILoadBalancer
    {
        private readonly ILogger<LoadBalancer> _logger;
        private readonly IHealthChecker _healthChecker;
        private readonly IStrategyProvider _strategyProvider;

        public LoadBalancer(ILogger<LoadBalancer> logger, IHealthChecker healthChecker, IStrategyProvider strategyProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _healthChecker = healthChecker;
            _strategyProvider = strategyProvider;
        }

        public Uri SelectBackend()
        {
            var healthy = _healthChecker.GetHealthyBackends();
            if (healthy.Count == 0) throw new InvalidOperationException("No healthy backends available.");
            return _strategyProvider.Current.SelectBackend(healthy);
        }
    }
}