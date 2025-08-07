namespace LoadBalancerProject.Configuration
{
    using Microsoft.Extensions.Logging;
    using System.Threading;

    /// <summary>
    /// Thread-safe in-memory implementation of <see cref="IDynamicConfig"/>.
    /// </summary>
    public sealed class InMemoryDynamicConfig : IDynamicConfig
    {
        private string _strategy;
        private int _interval;
        private readonly ILogger<InMemoryDynamicConfig> _logger;

        /// <summary>
        /// Creates a new configuration instance.
        /// </summary>
        /// <param name="initialStrategy">The initial strategy name.</param>
        /// <param name="initialInterval">The initial health check interval in seconds.</param>
        /// <param name="logger">The logger.</param>
        public InMemoryDynamicConfig(
            string initialStrategy,
            int initialInterval,
            ILogger<InMemoryDynamicConfig> logger)
        {
            _strategy = initialStrategy;
            _interval = initialInterval;
            _logger = logger;
        }

        /// <inheritdoc/>
        public string Strategy
        {
            get => Volatile.Read(ref _strategy);
            set
            {
                Interlocked.Exchange(ref _strategy, value);
                _logger.LogInformation("Strategy updated to {Strategy}", value);
            }
        }

        /// <inheritdoc/>
        public int HealthCheckIntervalSeconds
        {
            get => Volatile.Read(ref _interval);
            set
            {
                Interlocked.Exchange(ref _interval, value);
                _logger.LogInformation("Health check interval updated to {Interval} seconds", value);
            }
        }
    }
}
