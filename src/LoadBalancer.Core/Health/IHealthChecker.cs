namespace LoadBalancerProject.Health
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Checks backend health and provides the current healthy list.
    /// </summary>
    public interface IHealthChecker
    {
        /// <summary>
        /// Gets the list of currently healthy backends.
        /// </summary>
        /// <returns>A read-only list of healthy backend URIs.</returns>
        IReadOnlyList<Uri> GetHealthyBackends();
    }
}
