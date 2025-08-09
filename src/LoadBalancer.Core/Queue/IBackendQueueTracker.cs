namespace LoadBalancerProject.Queue
{
    using System;

    /// <summary>
    /// Tracks the current number of queued or active connections for each backend server.
    /// </summary>
    public interface IBackendQueueTracker
    {
        /// <summary>
        /// Increments the queue length for the specified backend.
        /// </summary>
        /// <param name="backend">The backend server URI.</param>
        void Increment(Uri backend);

        /// <summary>
        /// Decrements the queue length for the specified backend.
        /// </summary>
        /// <param name="backend">The backend server URI.</param>
        void Decrement(Uri backend);

        /// <summary>
        /// Gets the current queue length for the specified backend.
        /// </summary>
        /// <param name="backend">The backend server URI.</param>
        /// <returns>The number of active or queued requests for this backend.</returns>
        int GetQueueLength(Uri backend);
    }
}
