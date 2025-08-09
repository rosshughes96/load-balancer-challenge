namespace LoadBalancerProject.LoadBalancing.Strategies
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Defines a backend selection strategy for distributing requests.
    /// </summary>
    public interface IBackendSelectorStrategy
    {
        /// <summary>
        /// Selects the next backend from the provided list of healthy backends.
        /// </summary>
        /// <param name="healthyBackends">The list of healthy backend URIs.</param>
        /// <returns>The URI of the selected backend.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if no healthy backends are available.
        /// </exception>
        Uri SelectBackend(IReadOnlyList<Uri> healthyBackends);
    }
}
