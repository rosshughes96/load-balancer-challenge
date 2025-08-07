namespace LoadBalancerProject.Metrics
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Tracks active and total connections to backends.
    /// </summary>
    public interface IConnectionMetrics
    {
        /// <summary>
        /// Called when a connection starts to the given backend.
        /// </summary>
        /// <param name="backend">The backend URI.</param>
        void OnConnectionStart(Uri backend);

        /// <summary>
        /// Called when a connection ends to the given backend.
        /// </summary>
        /// <param name="backend">The backend URI.</param>
        void OnConnectionEnd(Uri backend);

        /// <summary>
        /// Creates a snapshot of the current metrics.
        /// </summary>
        /// <returns>A snapshot of connection metrics.</returns>
        MetricsSnapshot Snapshot();
    }

    /// <summary>
    /// Represents metrics for a single backend.
    /// </summary>
    /// <param name="Backend">The backend URI as a string.</param>
    /// <param name="Active">The number of active connections.</param>
    /// <param name="Total">The total connections served.</param>
    public record BackendMetrics(string Backend, int Active, long Total);

    /// <summary>
    /// Represents overall metrics.
    /// </summary>
    /// <param name="Backends">The metrics for each backend.</param>
    /// <param name="ActiveAll">The total active connections across all backends.</param>
    /// <param name="TotalAll">The total connections served across all backends.</param>
    public record MetricsSnapshot(IReadOnlyList<BackendMetrics> Backends, int ActiveAll, long TotalAll);
}
