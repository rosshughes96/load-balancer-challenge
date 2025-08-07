namespace LoadBalancerProject.Strategies
{
    using LoadBalancerProject.Configuration;
    using LoadBalancerProject.LoadBalancing.Strategies;
    using Microsoft.Extensions.Logging;
    using System;

    /// <summary>
    /// Chooses and provides a backend selection strategy based on dynamic configuration.
    /// </summary>
    public sealed class StrategyProvider : IStrategyProvider
    {
        private readonly IDynamicConfig _config;
        private readonly RoundRobinStrategy _roundRobin;
        private readonly LeastQueueStrategy _leastQueue;
        private readonly ILogger<StrategyProvider> _logger;

        private IBackendSelectorStrategy _current;

        /// <summary>
        /// Creates a new strategy provider.
        /// </summary>
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

        /// <inheritdoc/>
        public IBackendSelectorStrategy Current => _current;

        /// <inheritdoc/>
        public void Refresh()
        {
            _current = Resolve(_config.Strategy);
            _logger.LogInformation("Strategy changed to {Strategy}", _config.Strategy);
        }

        /// <summary>
        /// Resolves a strategy name to its implementation.
        /// Defaults to round-robin if name is unknown.
        /// </summary>
        private IBackendSelectorStrategy Resolve(string name) =>
            name?.Equals("LeastQueue", StringComparison.OrdinalIgnoreCase) == true
                ? _leastQueue
                : _roundRobin;
    }
}
