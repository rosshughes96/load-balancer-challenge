namespace LoadBalancerProject.LoadBalancing.Strategies
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Selects a backend from the available healthy list.
    /// </summary>
    public interface IBackendSelectorStrategy
    {
        /// <summary>
        /// Selects one backend from the given healthy backends.
        /// </summary>
        /// <param name="healthyBackends">The healthy backends to choose from.</param>
        /// <returns>The selected backend URI.</returns>
        Uri SelectBackend(IReadOnlyList<Uri> healthyBackends);
    }
}
