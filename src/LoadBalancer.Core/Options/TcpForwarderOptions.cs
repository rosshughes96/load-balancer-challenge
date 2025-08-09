namespace LoadBalancerProject.Options
{
    /// <summary>
    /// Configuration options for the TCP forwarder.
    /// </summary>
    public sealed class TcpForwarderOptions
    {
        /// <summary>
        /// Gets or sets the maximum number of concurrent client connections allowed.
        /// </summary>
        public int MaxConcurrentConnections { get; set; } = 100;

        /// <summary>
        /// Gets or sets the idle timeout for a connection in seconds.
        /// </summary>
        public int IdleTimeoutSeconds { get; set; } = 15;

        /// <summary>
        /// Gets or sets the maximum lifetime for a connection in seconds.
        /// </summary>
        public int MaxLifetimeSeconds { get; set; } = 300;

        /// <summary>
        /// Gets or sets the TCP buffer size in bytes.
        /// </summary>
        public int BufferSize { get; set; } = 8192;
    }
}
