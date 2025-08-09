namespace LoadBalancerProject.Backends
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Registry for managing the set of configured backend endpoints.
    /// </summary>
    /// <remarks>
    /// Implementations should be thread-safe. The <see cref="List"/> method
    /// should return a stable, deterministic order (e.g., lexicographically)
    /// so strategies like Round Robin behave predictably.
    /// </remarks>
    public interface IBackendRegistry
    {
        /// <summary>
        /// Adds a backend endpoint to the registry.
        /// </summary>
        /// <param name="backend">The backend URI (e.g., tcp://127.0.0.1:5001).</param>
        /// <returns><see langword="true"/> if the backend was added; otherwise <see langword="false"/> if it already existed.</returns>
        bool Add(Uri backend);

        /// <summary>
        /// Removes a backend endpoint from the registry.
        /// </summary>
        /// <param name="backend">The backend URI to remove.</param>
        /// <returns><see langword="true"/> if the backend was removed; otherwise <see langword="false"/>.</returns>
        bool Remove(Uri backend);

        /// <summary>
        /// Checks whether a backend endpoint exists in the registry.
        /// </summary>
        /// <param name="backend">The backend URI to check.</param>
        /// <returns><see langword="true"/> if the backend exists; otherwise <see langword="false"/>.</returns>
        bool Contains(Uri backend);

        /// <summary>
        /// Returns a sorted, read-only snapshot of all registered backends.
        /// </summary>
        /// <returns>A read-only list of backend URIs.</returns>
        IReadOnlyList<Uri> List();

        /// <summary>
        /// Replaces the entire registry with the specified set of backends.
        /// </summary>
        /// <param name="backends">The complete set of backends to set.</param>
        /// <remarks>
        /// Implementations may not be atomic w.r.t. readers; callers should tolerate a brief transition.
        /// </remarks>
        void SetAll(IEnumerable<Uri> backends);
    }
}
