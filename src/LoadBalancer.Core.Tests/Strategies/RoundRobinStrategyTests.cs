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
public class RoundRobinStrategyTests
{
    [Test]
    public void SelectBackend_WithBackends_Should_CycleStrictly()
    {
        // Arrange
        var sut = new RoundRobinStrategy();
        var list = new List<Uri>
        {
            new("tcp://127.0.0.1:5001"),
            new("tcp://127.0.0.1:5002"),
            new("tcp://127.0.0.1:5003")
        };

        // Act
        var p1 = sut.SelectBackend(list);
        var p2 = sut.SelectBackend(list);
        var p3 = sut.SelectBackend(list);
        var p4 = sut.SelectBackend(list);

        // Assert
        Assert.That(p1, Is.EqualTo(list[0]));
        Assert.That(p2, Is.EqualTo(list[1]));
        Assert.That(p3, Is.EqualTo(list[2]));
        Assert.That(p4, Is.EqualTo(list[0]));
    }

    [Test]
    public void SelectBackend_When_Concurrent_Should_BeEvenlyDistributed()
    {
        // Arrange
        var sut = new RoundRobinStrategy();
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
            var u = sut.SelectBackend(list);
            var ix = list.IndexOf(u);
            Interlocked.Increment(ref counts[ix]);
        });

        // Assert
        foreach (var c in counts)
        {
            Assert.That(c, Is.InRange(1800, 2200));
        }
    }

    [Test]
    public void SelectBackend_When_ListEmpty_Should_Throw()
    {
        // Arrange
        var sut = new RoundRobinStrategy();
        var empty = new List<Uri>();

        // Act / Assert
        Assert.That(() => sut.SelectBackend(empty), Throws.InvalidOperationException);
    }
}