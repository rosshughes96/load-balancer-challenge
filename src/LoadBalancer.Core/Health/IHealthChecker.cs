namespace LoadBalancerProject.Health
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Defines a service that can provide the current set of healthy backend endpoints.
    /// </summary>
    /// <remarks>
    /// Implementations must be thread-safe and return an immutable snapshot of healthy backends.
    /// </remarks>
    public interface IHealthChecker
    {
        /// <summary>
        /// Retrieves a sorted, read-only list of backends currently considered healthy.
        /// </summary>
        /// <returns>A read-only list of <see cref="Uri"/> instances.</returns>
        IReadOnlyList<Uri> GetHealthyBackends();
    }
}
