namespace LoadBalancer.Core.LoadBalancing.Strategies
{
    using System;
    using LoadBalancerProject.Configuration;
    using LoadBalancerProject.LoadBalancing.Strategies;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Implementation of <see cref="IStrategyProvider"/> that can switch between multiple strategies dynamically.
    /// </summary>
    public sealed class StrategyProvider : IStrategyProvider
    {
        private readonly IDynamicConfig _config;
        private readonly RoundRobinStrategy _roundRobin;
        private readonly LeastQueueStrategy _leastQueue;
        private readonly ILogger<StrategyProvider> _logger;

        private IBackendSelectorStrategy _current;

        /// <summary>
        /// Initializes a new instance of the <see cref="StrategyProvider"/> class.
        /// </summary>
        /// <param name="config">The dynamic configuration service.</param>
        /// <param name="roundRobin">The round robin strategy instance.</param>
        /// <param name="leastQueue">The least queue strategy instance.</param>
        /// <param name="logger">The logger for diagnostic output.</param>
        /// <exception cref="ArgumentNullException">Thrown if any dependency is null.</exception>
        public StrategyProvider(
            IDynamicConfig config,
            RoundRobinStrategy roundRobin,
            LeastQueueStrategy leastQueue,
            ILogger<StrategyProvider> logger)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _roundRobin = roundRobin ?? throw new ArgumentNullException(nameof(roundRobin));
            _leastQueue = leastQueue ?? throw new ArgumentNullException(nameof(leastQueue));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

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
        /// Resolves the correct strategy instance based on its name.
        /// Defaults to <see cref="RoundRobinStrategy"/> if no match is found.
        /// </summary>
        /// <param name="name">The strategy name from configuration.</param>
        /// <returns>An <see cref="IBackendSelectorStrategy"/> instance.</returns>
        private IBackendSelectorStrategy Resolve(string name) =>
            name?.Equals("LeastQueue", StringComparison.OrdinalIgnoreCase) == true
                ? _leastQueue
                : _roundRobin;
    }
}
