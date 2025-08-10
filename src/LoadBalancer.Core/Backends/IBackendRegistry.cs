namespace LoadBalancerProject.Backends
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Manages backend addresses.
    /// </summary>
    public interface IBackendRegistry
    {
        /// <summary>
        /// Tries to add a backend.
        /// </summary>
        /// <param name="backend">The backend address.</param>
        /// <returns>True if added, otherwise false.</returns>
        bool Add(Uri backend);

        /// <summary>
        /// Tries to remove a backend.
        /// </summary>
        /// <param name="backend">The backend address.</param>
        /// <returns>True if removed, otherwise false.</returns>
        bool Remove(Uri backend);

        /// <summary>
        /// Checks if a backend exists.
        /// </summary>
        /// <param name="backend">The backend address.</param>
        /// <returns>True if it exists, otherwise false.</returns>
        bool Contains(Uri backend);

        /// <summary>
        /// Gets all backends.
        /// </summary>
        /// <returns>A read-only list of backends.</returns>
        IReadOnlyList<Uri> List();

    }
}
