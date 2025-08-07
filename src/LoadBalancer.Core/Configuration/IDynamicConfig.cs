namespace LoadBalancerProject.Configuration
{
    public interface IDynamicConfig
    {
        string Strategy { get; set; }
        int HealthCheckIntervalSeconds { get; set; }
    }
}