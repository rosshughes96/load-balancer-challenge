namespace LoadBalancerProject.Configuration
{
    /// <summary>
    /// Represents a dynamic load balancer configuration that can be updated at runtime.
    /// </summary>
    /// <remarks>
    /// Implementations must be thread-safe for concurrent reads and writes.
    /// </remarks>
    public interface IDynamicConfig
    {
        /// <summary>
        /// Gets or sets the current backend selection strategy name.
        /// </summary>
        string Strategy { get; set; }

        /// <summary>
        /// Gets or sets the interval, in seconds, between health checks.
        /// </summary>
        int HealthCheckIntervalSeconds { get; set; }
    }
}
