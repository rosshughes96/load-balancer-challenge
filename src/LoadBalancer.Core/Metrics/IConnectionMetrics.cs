namespace LoadBalancerProject.Metrics
{
    public interface IConnectionMetrics
    {
        void OnConnectionStart(Uri backend);
        void OnConnectionEnd(Uri backend);
        MetricsSnapshot Snapshot();
    }

    public record BackendMetrics(string Backend, int Active, long Total);
    public record MetricsSnapshot(IReadOnlyList<BackendMetrics> Backends, int ActiveAll, long TotalAll);
}