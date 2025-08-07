namespace LoadBalancerProject.Options
{
    public class TcpForwarderOptions
    {
        public int MaxConcurrentConnections { get; set; } = 100;
        public int IdleTimeoutSeconds { get; set; } = 15;
        public int MaxLifetimeSeconds { get; set; } = 300;
        public int BufferSize { get; set; } = 8192;
    }
}