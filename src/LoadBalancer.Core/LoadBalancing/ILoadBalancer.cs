namespace LoadBalancerProject.LoadBalancing
{
    using System;

    /// <summary>
    /// Selects a backend endpoint to handle the next incoming connection.
    /// </summary>
    /// <remarks>
    /// Implementations are expected to be thread-safe. The selection policy is provided
    /// by the implementation (e.g., Round Robin, Least Queue). If no healthy backends
    /// are available, <see cref="InvalidOperationException"/> should be thrown.
    /// </remarks>
    public interface ILoadBalancer
    {
        /// <summary>
        /// Selects a backend <see cref="Uri"/> from the current healthy set.
        /// </summary>
        /// <returns>
        /// The <see cref="Uri"/> of the selected backend.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when there are no healthy backends available.
        /// </exception>
        Uri SelectBackend();
    }
}
