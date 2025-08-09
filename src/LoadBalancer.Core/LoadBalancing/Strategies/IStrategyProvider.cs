namespace LoadBalancer.Core.LoadBalancing.Strategies
{
    using LoadBalancerProject.LoadBalancing.Strategies;

    /// <summary>
    /// Provides and manages the currently active backend selection strategy.
    /// </summary>
    public interface IStrategyProvider
    {
        /// <summary>
        /// Gets the currently active backend selection strategy.
        /// </summary>
        IBackendSelectorStrategy Current { get; }

        /// <summary>
        /// Refreshes the active strategy based on the current configuration.
        /// </summary>
        void Refresh();
    }
}
