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


namespace LoadBalancerProject.Tests.Queue;

[Category("Unit")]
[TestFixture]
public class BackendQueueTrackerTests
{
    [Test]
    public void Increment_When_Called_Should_IncreaseLength()
    {
        // Arrange
        var sut = new BackendQueueTracker();
        var a = new Uri("tcp://127.0.0.1:5001");

        // Act
        sut.Increment(a);
        sut.Increment(a);
        var len = sut.GetQueueLength(a);

        // Assert
        Assert.That(len, Is.EqualTo(2));
    }

    [Test]
    public void Decrement_When_AtZero_Should_NotGoNegative()
    {
        // Arrange
        var sut = new BackendQueueTracker();
        var a = new Uri("tcp://127.0.0.1:5001");

        // Act
        sut.Decrement(a);
        var len = sut.GetQueueLength(a);

        // Assert
        Assert.That(len, Is.EqualTo(0));
    }
}