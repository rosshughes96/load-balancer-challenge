namespace LoadBalancerProject.LoadBalancing.Strategies
{
    using System;
    using System.Threading;
    using LoadBalancerProject.Configuration;
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

        // Use Volatile.Read/Write for memory visibility across threads
        private IBackendSelectorStrategy _current;

        /// <summary>
        /// Initializes a new instance of the <see cref="StrategyProvider"/> class.
        /// </summary>
        /// <param name="config">The dynamic configuration source.</param>
        /// <param name="roundRobin">The round robin strategy instance.</param>
        /// <param name="leastQueue">The least queue strategy instance.</param>
        /// <param name="logger">The logger instance.</param>
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
        public IBackendSelectorStrategy Current => Volatile.Read(ref _current);

        /// <inheritdoc/>
        public void Refresh()
        {
            var next = Resolve(_config.Strategy);
            Volatile.Write(ref _current, next);
            _logger.LogInformation("Strategy changed to {Strategy}", _config.Strategy);
        }

        /// <summary>
        /// Resolves the correct strategy instance based on its name.
        /// Defaults to round robin if the name does not match.
        /// </summary>
        private IBackendSelectorStrategy Resolve(string name) =>
            name?.Equals("LeastQueue", StringComparison.OrdinalIgnoreCase) == true
                ? _leastQueue
                : _roundRobin;
    }
}
