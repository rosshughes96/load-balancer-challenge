namespace LoadBalancerProject.Backends
{
    public interface IBackendRegistry
    {
        bool Add(Uri backend);
        bool Remove(Uri backend);
        IReadOnlyList<Uri> List();
        void SetAll(IEnumerable<Uri> backends);
        bool Contains(Uri backend);
    }
}