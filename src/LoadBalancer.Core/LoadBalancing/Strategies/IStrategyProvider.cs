namespace LoadBalancerProject.LoadBalancing.Strategies
{
    /// <summary>
    /// Provides the currently active backend selection strategy and allows refreshing it.
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
