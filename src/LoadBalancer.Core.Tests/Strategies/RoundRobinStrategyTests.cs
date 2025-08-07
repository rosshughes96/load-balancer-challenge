using LoadBalancerProject.LoadBalancing.Strategies;
using NUnit.Framework;


namespace LoadBalancerProject.Tests.Strategies;

[TestFixture]
public class RoundRobinStrategyTests
{
    [Test]
    public void SelectBackend_Cycles_Strictly_In_Order()
    {
        // Arrange
        var rr = new RoundRobinStrategy();
        var list = new List<Uri>
        {
            new("tcp://127.0.0.1:5001"),
            new("tcp://127.0.0.1:5002"),
            new("tcp://127.0.0.1:5003")
        };

        // Act
        var p1 = rr.SelectBackend(list);
        var p2 = rr.SelectBackend(list);
        var p3 = rr.SelectBackend(list);
        var p4 = rr.SelectBackend(list);

        // Assert
        Assert.That(p1, Is.EqualTo(list[0]));
        Assert.That(p2, Is.EqualTo(list[1]));
        Assert.That(p3, Is.EqualTo(list[2]));
        Assert.That(p4, Is.EqualTo(list[0]));
    }

    [Test]
    public void SelectBackend_Is_Thread_Safe_And_Balanced()
    {
        // Arrange
        var rr = new RoundRobinStrategy();
        var list = new List<Uri>
        {
            new("tcp://127.0.0.1:5001"),
            new("tcp://127.0.0.1:5002"),
            new("tcp://127.0.0.1:5003"),
            new("tcp://127.0.0.1:5004"),
            new("tcp://127.0.0.1:5005"),
        };
        var counts = new int[list.Count];

        // Act
        System.Threading.Tasks.Parallel.For(0, 10_000, _ =>
        {
            var u = rr.SelectBackend(list);
            var ix = list.IndexOf(u);
            Interlocked.Increment(ref counts[ix]);
        });

        // Assert (each bucket ~ 2000, allow some jitter)
        foreach (var c in counts)
        {
            Assert.That(c, Is.InRange(1800, 2200));
        }
    }

    [Test]
    public void SelectBackend_Throws_On_Empty_List()
    {
        // Arrange
        var rr = new RoundRobinStrategy();
        var empty = new List<Uri>();

        // Act/Assert
        Assert.That(() => rr.SelectBackend(empty), Throws.InvalidOperationException);
    }
}