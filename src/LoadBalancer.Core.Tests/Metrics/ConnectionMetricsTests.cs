using LoadBalancerProject.Metrics;
using NUnit.Framework;


namespace LoadBalancerProject.Tests.Metrics;

[TestFixture]
public class ConnectionMetricsTests
{
    [Test]
    public void Snapshot_Tracks_Active_And_Total()
    {
        // Arrange
        var m = new ConnectionMetrics();
        var a = new Uri("tcp://127.0.0.1:5001");
        var b = new Uri("tcp://127.0.0.1:5002");

        // Act
        m.OnConnectionStart(a);
        m.OnConnectionStart(a);
        m.OnConnectionStart(b);
        var s1 = m.Snapshot();

        m.OnConnectionEnd(a);
        var s2 = m.Snapshot();

        // Assert
        Assert.That(s1.Backends.Count, Is.EqualTo(2));
        Assert.That(s1.ActiveAll, Is.EqualTo(3));
        Assert.That(s1.TotalAll, Is.EqualTo(3));
        var a1 = s1.Backends.Single(x => x.Backend.EndsWith(":5001/"));
        var b1 = s1.Backends.Single(x => x.Backend.EndsWith(":5002/"));
        Assert.That(a1.Active, Is.EqualTo(2));
        Assert.That(a1.Total, Is.EqualTo(2));
        Assert.That(b1.Active, Is.EqualTo(1));
        Assert.That(b1.Total, Is.EqualTo(1));

        Assert.That(s2.ActiveAll, Is.EqualTo(2));
        Assert.That(s2.TotalAll, Is.EqualTo(3));
        var a2 = s2.Backends.Single(x => x.Backend.EndsWith(":5001/"));
        Assert.That(a2.Active, Is.EqualTo(1));
        Assert.That(a2.Total, Is.EqualTo(2));
    }
}