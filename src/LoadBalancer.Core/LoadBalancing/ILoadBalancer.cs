namespace LoadBalancerProject.LoadBalancing
{
    public interface ILoadBalancer
    {
        Uri SelectBackend();
    }
}