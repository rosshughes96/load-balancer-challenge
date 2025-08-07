namespace LoadBalancerProject.LoadBalancing.Strategies
{
    public interface IBackendSelectorStrategy
    {
        Uri SelectBackend(IReadOnlyList<Uri> healthyBackends);
    }
}