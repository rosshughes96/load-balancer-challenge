namespace LoadBalancerProject.Configuration
{
    /// <summary>
    /// Represents settings that can change at runtime.
    /// </summary>
    public interface IDynamicConfig
    {
        /// <summary>
        /// The name of the load balancing strategy.
        /// </summary>
        string Strategy { get; set; }

        /// <summary>
        /// The health check interval in seconds.
        /// </summary>
        int HealthCheckIntervalSeconds { get; set; }
    }
}
