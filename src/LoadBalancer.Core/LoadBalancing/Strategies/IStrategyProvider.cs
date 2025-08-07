namespace LoadBalancerProject.Strategies
{
    using LoadBalancerProject.LoadBalancing.Strategies;

    /// <summary>
    /// Provides the currently selected backend selection strategy.
    /// </summary>
    public interface IStrategyProvider
    {
        /// <summary>
        /// The current strategy.
        /// </summary>
        IBackendSelectorStrategy Current { get; }

        /// <summary>
        /// Refreshes the current strategy from configuration.
        /// </summary>
        void Refresh();
    }
}
