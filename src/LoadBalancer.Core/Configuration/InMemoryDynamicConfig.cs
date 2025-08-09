namespace LoadBalancerProject.Configuration
{
    using System;
    using System.Threading;

    /// <summary>
    /// Thread-safe in-memory implementation of <see cref="IDynamicConfig"/> that can be modified at runtime.
    /// </summary>
    /// <remarks>
    /// This implementation uses <see cref="Volatile"/> and <see cref="Interlocked"/> to ensure atomic reads/writes
    /// for concurrent access. It is suitable for scenarios where configuration changes are relatively infrequent
    /// compared to reads (e.g., updating via API while the load balancer is running).
    /// </remarks>
    public sealed class InMemoryDynamicConfig : IDynamicConfig
    {
        private string _strategy;
        private int _interval;

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryDynamicConfig"/> class.
        /// </summary>
        /// <param name="initialStrategy">Initial backend selection strategy.</param>
        /// <param name="initialInterval">Initial health check interval in seconds.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="initialStrategy"/> is null.</exception>
        public InMemoryDynamicConfig(string initialStrategy, int initialInterval)
        {
            _strategy = initialStrategy ?? throw new ArgumentNullException(nameof(initialStrategy));
            _interval = initialInterval;
        }

        /// <inheritdoc />
        public string Strategy
        {
            get => Volatile.Read(ref _strategy);
            set => Interlocked.Exchange(ref _strategy, value ?? throw new ArgumentNullException(nameof(value)));
        }

        /// <inheritdoc />
        public int HealthCheckIntervalSeconds
        {
            get => Volatile.Read(ref _interval);
            set => Interlocked.Exchange(ref _interval, value);
        }
    }
}
