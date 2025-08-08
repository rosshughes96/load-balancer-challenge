using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using LoadBalancerProject.Backends;
using LoadBalancerProject.Configuration;
using LoadBalancerProject.Health;
using LoadBalancerProject.LoadBalancing;
using LoadBalancerProject.LoadBalancing.Strategies;
using LoadBalancerProject.Metrics;
using LoadBalancerProject.Queue;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;


namespace LoadBalancerProject.Tests.Strategies;

[Category("Unit")]
[TestFixture]
public class LeastQueueStrategyTests
{
    [Test]
    public void SelectBackend_When_QueuesDiffer_Should_PickShortest()
    {
        // Arrange
        var a = new Uri("tcp://127.0.0.1:5001");
        var b = new Uri("tcp://127.0.0.1:5002");
        var c = new Uri("tcp://127.0.0.1:5003");

        var tracker = Substitute.For<IBackendQueueTracker>();
        tracker.GetQueueLength(a).Returns(3);
        tracker.GetQueueLength(b).Returns(1);
        tracker.GetQueueLength(c).Returns(2);

        var sut = new LeastQueueStrategy(tracker);
        var list = new List<Uri> { a, b, c };

        // Act
        var pick = sut.SelectBackend(list);

        // Assert
        Assert.That(pick, Is.EqualTo(b));
    }
}