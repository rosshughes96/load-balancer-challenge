namespace LoadBalancerProject.Health
{
    public interface IHealthChecker
    {
        IReadOnlyList<Uri> GetHealthyBackends();
    }
}