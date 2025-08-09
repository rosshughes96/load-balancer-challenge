namespace LoadBalancerProject.Options
{
    using System.Collections.Generic;

    /// <summary>
    /// Configuration options for the load balancer.
    /// </summary>
    public sealed class LoadBalancerOptions
    {
        /// <summary>
        /// Gets or sets the list of backend endpoints (e.g., tcp://localhost:5001).
        /// </summary>
        public List<string> Backends { get; set; } = new();

        /// <summary>
        /// Gets or sets the health check interval in seconds.
        /// </summary>
        public int HealthCheckIntervalSeconds { get; set; } = 5;

        /// <summary>
        /// Gets or sets the backend selection strategy name (e.g., "RoundRobin", "LeastQueue").
        /// </summary>
        public string Strategy { get; set; } = "RoundRobin";

        /// <summary>
        /// Gets or sets the TCP listener port for the load balancer.
        /// </summary>
        public int ListenPort { get; set; } = 5000;
    }
}
