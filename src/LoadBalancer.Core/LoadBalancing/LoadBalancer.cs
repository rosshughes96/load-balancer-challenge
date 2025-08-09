namespace LoadBalancerProject.LoadBalancing
{
    using System;
    using LoadBalancerProject.Health;
    using LoadBalancerProject.LoadBalancing.Strategies;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Default load balancer that selects a backend from the set of healthy backends
    /// using the currently configured strategy.
    /// </summary>
    /// <remarks>
    /// This type is thread-safe provided its dependencies are thread-safe:
    /// <list type="bullet">
    /// <item><description><see cref="IHealthChecker"/> must return a consistent, immutable snapshot of healthy backends.</description></item>
    /// <item><description><see cref="IStrategyProvider"/> must expose a stable <c>Current</c> strategy instance.</description></item>
    /// </list>
    /// </remarks>
    public sealed class LoadBalancer : ILoadBalancer
    {
        private readonly ILogger<LoadBalancer> _logger;
        private readonly IHealthChecker _healthChecker;
        private readonly IStrategyProvider _strategyProvider;

        /// <summary>
        /// Creates a new <see cref="LoadBalancer"/>.
        /// </summary>
        /// <param name="logger">Logger for diagnostic output.</param>
        /// <param name="healthChecker">Provides the current set of healthy backends.</param>
        /// <param name="strategyProvider">Provides the active selection strategy.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if any argument is <see langword="null"/>.
        /// </exception>
        public LoadBalancer(
            ILogger<LoadBalancer> logger,
            IHealthChecker healthChecker,
            IStrategyProvider strategyProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _healthChecker = healthChecker ?? throw new ArgumentNullException(nameof(healthChecker));
            _strategyProvider = strategyProvider ?? throw new ArgumentNullException(nameof(strategyProvider));
        }

        /// <summary>
        /// Selects a backend from the current healthy set using the active strategy.
        /// </summary>
        /// <returns>The selected backend <see cref="Uri"/>.</returns>
        /// <exception cref="InvalidOperationException">No healthy backends are available.</exception>
        public Uri SelectBackend()
        {
            // Obtain a snapshot of healthy backends. This should be an immutable list.
            var healthy = _healthChecker.GetHealthyBackends();

            if (healthy.Count == 0)
            {
                _logger.LogWarning("SelectBackend called but no healthy backends are available.");
                throw new InvalidOperationException("No healthy backends available.");
            }

            var strategy = _strategyProvider.Current;
            var selected = strategy.SelectBackend(healthy);

            // Debug-level to avoid log noise; useful during troubleshooting/demos.
            _logger.LogDebug("Strategy {Strategy} selected backend {Backend}", strategy.GetType().Name, selected);

            return selected;
        }
    }
}
