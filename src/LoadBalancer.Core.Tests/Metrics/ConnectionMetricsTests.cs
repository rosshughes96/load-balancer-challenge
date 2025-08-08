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


namespace LoadBalancerProject.Tests.Metrics;

[Category("Unit")]
[TestFixture]
public class ConnectionMetricsTests
{
    [Test]
    public void Snapshot_When_ConnectionsChange_Should_ReflectCounts()
    {
        // Arrange
        var sut = new ConnectionMetrics();
        var a = new Uri("tcp://127.0.0.1:5001");
        var b = new Uri("tcp://127.0.0.1:5002");

        // Act
        sut.OnConnectionStart(a);
        sut.OnConnectionStart(a);
        sut.OnConnectionStart(b);
        var s1 = sut.Snapshot();

        sut.OnConnectionEnd(a);
        var s2 = sut.Snapshot();

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