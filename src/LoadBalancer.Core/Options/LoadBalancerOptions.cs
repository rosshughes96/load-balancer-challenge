namespace LoadBalancerProject.Options
{
    public class LoadBalancerOptions
    {
        public List<string> Backends { get; set; } = new();
        public int HealthCheckIntervalSeconds { get; set; } = 5;
        public string Strategy { get; set; } = "RoundRobin";
        public int ListenPort { get; set; } = 5000;
    }
}