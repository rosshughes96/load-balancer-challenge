namespace LoadBalancerProject.LoadBalancing
{
    using System;

    /// <summary>
    /// Chooses a backend to handle a request.
    /// </summary>
    public interface ILoadBalancer
    {
        /// <summary>
        /// Selects a backend from the available healthy backends.
        /// </summary>
        /// <returns>The selected backend URI.</returns>
        Uri SelectBackend();
    }
}
