using LoadBalancerProject.Queue;
using NUnit.Framework;


namespace LoadBalancerProject.Tests.Queue;

[TestFixture]
public class BackendQueueTrackerTests
{
    [Test]
    public void Increment_And_Decrement_Modify_Length()
    {
        // Arrange
        var tracker = new BackendQueueTracker();
        var a = new Uri("tcp://127.0.0.1:5001");

        // Act
        tracker.Increment(a);
        tracker.Increment(a);
        var len2 = tracker.GetQueueLength(a);
        tracker.Decrement(a);
        var len1 = tracker.GetQueueLength(a);
        tracker.Decrement(a);
        var len0 = tracker.GetQueueLength(a);
        tracker.Decrement(a);
        var lenStill0 = tracker.GetQueueLength(a);

        // Assert
        Assert.That(len2, Is.EqualTo(2));
        Assert.That(len1, Is.EqualTo(1));
        Assert.That(len0, Is.EqualTo(0));
        Assert.That(lenStill0, Is.EqualTo(0));
    }
}