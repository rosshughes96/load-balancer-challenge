namespace LoadBalancerProject.Queue
{
    using System;

    /// <summary>
    /// Tracks the number of active connections for each backend.
    /// </summary>
    public interface IBackendQueueTracker
    {
        /// <summary>
        /// Increments the queue length for the given backend.
        /// </summary>
        /// <param name="backend">The backend URI.</param>
        void Increment(Uri backend);

        /// <summary>
        /// Decrements the queue length for the given backend.
        /// </summary>
        /// <param name="backend">The backend URI.</param>
        void Decrement(Uri backend);

        /// <summary>
        /// Gets the current queue length for the given backend.
        /// </summary>
        /// <param name="backend">The backend URI.</param>
        /// <returns>The number of active connections.</returns>
        int GetQueueLength(Uri backend);
    }
}
