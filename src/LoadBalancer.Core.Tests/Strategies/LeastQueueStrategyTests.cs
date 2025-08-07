using LoadBalancerProject.LoadBalancing.Strategies;
using LoadBalancerProject.Queue;
using NSubstitute;
using NUnit.Framework;


namespace LoadBalancerProject.Tests.Strategies;

[TestFixture]
public class LeastQueueStrategyTests
{
    [Test]
    public void SelectBackend_Picks_Shortest_Queue()
    {
        // Arrange
        var a = new Uri("tcp://127.0.0.1:5001");
        var b = new Uri("tcp://127.0.0.1:5002");
        var c = new Uri("tcp://127.0.0.1:5003");

        var tracker = Substitute.For<IBackendQueueTracker>();
        tracker.GetQueueLength(a).Returns(3);
        tracker.GetQueueLength(b).Returns(1);
        tracker.GetQueueLength(c).Returns(2);

        var s = new LeastQueueStrategy(tracker);
        var list = new List<Uri> { a, b, c };

        // Act
        var pick = s.SelectBackend(list);

        // Assert
        Assert.That(pick, Is.EqualTo(b));
    }
}