namespace LoadBalancerProject.Metrics
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Tracks active and total connection counts for each backend.
    /// </summary>
    /// <remarks>
    /// Implementations must be thread-safe for concurrent updates and reads.
    /// </remarks>
    public interface IConnectionMetrics
    {
        /// <summary>
        /// Records the start of a connection to the specified backend.
        /// </summary>
        /// <param name="backend">The backend URI.</param>
        void OnConnectionStart(Uri backend);

        /// <summary>
        /// Records the end of a connection to the specified backend.
        /// </summary>
        /// <param name="backend">The backend URI.</param>
        void OnConnectionEnd(Uri backend);

        /// <summary>
        /// Retrieves a snapshot of current and total connection counts.
        /// </summary>
        /// <returns>A <see cref="MetricsSnapshot"/> containing per-backend and aggregate counts.</returns>
        MetricsSnapshot Snapshot();
    }

    /// <summary>
    /// Per-backend connection metrics.
    /// </summary>
    public record BackendMetrics(string Backend, int Active, long Total);

    /// <summary>
    /// Snapshot of connection metrics across all backends.
    /// </summary>
    public record MetricsSnapshot(IReadOnlyList<BackendMetrics> Backends, int ActiveAll, long TotalAll);
}
