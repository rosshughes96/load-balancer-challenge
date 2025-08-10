namespace LoadBalancerProject.Proxy.Refusal
{
    /// <summary>
    /// Controls how the service refuses client connections when no healthy backends exist.
    /// </summary>
    public enum RefusalMode
    {
        /// <summary>
        /// Immediately reset the TCP connection (RST).
        /// </summary>
        TcpReset,

        /// <summary>
        /// Attempt a graceful close (FIN).
        /// </summary>
        GracefulFin
    }
}
