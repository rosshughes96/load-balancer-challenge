namespace LoadBalancerProject.LoadBalancing
{
    using LoadBalancerProject.Health;
    using LoadBalancerProject.Strategies;
    using Microsoft.Extensions.Logging;
    using System;

    /// <summary>
    /// Default implementation of <see cref="ILoadBalancer"/>.
    /// </summary>
    public sealed class LoadBalancer : ILoadBalancer
    {
        private readonly ILogger<LoadBalancer> _logger;
        private readonly IHealthChecker _healthChecker;
        private readonly IStrategyProvider _strategyProvider;

        /// <summary>
        /// Creates a new load balancer instance.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="healthChecker">The health checker.</param>
        /// <param name="strategyProvider">The strategy provider.</param>
        public LoadBalancer(
            ILogger<LoadBalancer> logger,
            IHealthChecker healthChecker,
            IStrategyProvider strategyProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _healthChecker = healthChecker ?? throw new ArgumentNullException(nameof(healthChecker));
            _strategyProvider = strategyProvider ?? throw new ArgumentNullException(nameof(strategyProvider));
        }

        /// <inheritdoc/>
        public Uri SelectBackend()
        {
            var healthy = _healthChecker.GetHealthyBackends();

            if (healthy.Count == 0)
            {
                _logger.LogWarning("No healthy backends available to select");
                throw new InvalidOperationException("No healthy backends available.");
            }

            var selected = _strategyProvider.Current.SelectBackend(healthy);
            _logger.LogDebug("Selected backend {Backend}", selected);
            return selected;
        }
    }
}
