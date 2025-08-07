namespace LoadBalancerProject.Options
{
    /// <summary>
    /// Options for TCP forwarding behavior.
    /// </summary>
    public class TcpForwarderOptions
    {
        /// <summary>
        /// The maximum number of concurrent connections allowed.
        /// Default is 100.
        /// </summary>
        public int MaxConcurrentConnections { get; set; } = 100;

        /// <summary>
        /// The number of idle seconds before closing a connection.
        /// Default is 15.
        /// </summary>
        public int IdleTimeoutSeconds { get; set; } = 15;

        /// <summary>
        /// The maximum lifetime of a connection in seconds.
        /// Default is 300.
        /// </summary>
        public int MaxLifetimeSeconds { get; set; } = 300;

        /// <summary>
        /// The size of the buffer in bytes for network reads and writes.
        /// Default is 8192.
        /// </summary>
        public int BufferSize { get; set; } = 8192;
    }
}
