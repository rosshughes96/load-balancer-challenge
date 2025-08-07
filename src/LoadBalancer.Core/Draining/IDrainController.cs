namespace LoadBalancerProject.Draining
{
    using System;

    /// <summary>
    /// Manages safe removal (drain) state for backends.
    /// </summary>
    public interface IDrainController
    {
        /// <summary>
        /// Starts draining a backend. New traffic is stopped.
        /// </summary>
        /// <param name="backend">The backend URI.</param>
        /// <param name="timeout">An optional timeout before forced removal.</param>
        void BeginDrain(Uri backend, TimeSpan? timeout = null);

        /// <summary>
        /// Checks if a backend is draining.
        /// </summary>
        /// <param name="backend">The backend URI.</param>
        /// <returns>True if draining; otherwise false.</returns>
        bool IsDraining(Uri backend);

        /// <summary>
        /// Clears the drain state for a backend.
        /// </summary>
        /// <param name="backend">The backend URI.</param>
        void Clear(Uri backend);
    }
}
