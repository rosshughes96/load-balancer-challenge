namespace LoadBalancerProject.Options
{
    using System.Collections.Generic;

    /// <summary>
    /// Options for configuring the load balancer.
    /// </summary>
    public class LoadBalancerOptions
    {
        /// <summary>
        /// The list of backend addresses as strings.
        /// </summary>
        public List<string> Backends { get; set; } = new();

        /// <summary>
        /// The interval in seconds between health checks.
        /// Default is 5.
        /// </summary>
        public int HealthCheckIntervalSeconds { get; set; } = 5;

        /// <summary>
        /// The load balancing strategy name.
        /// Default is "RoundRobin".
        /// </summary>
        public string Strategy { get; set; } = "RoundRobin";

        /// <summary>
        /// The TCP port to listen on.
        /// Default is 5000.
        /// </summary>
        public int ListenPort { get; set; } = 6000;
    }
}
