namespace LoadBalancerProject.Queue
{
    public interface IBackendQueueTracker
    {
        void Increment(Uri backend);
        void Decrement(Uri backend);
        int GetQueueLength(Uri backend);
    }
}