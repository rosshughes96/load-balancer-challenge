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


namespace LoadBalancerProject.Tests.Configuration;

[Category("Unit")]
[TestFixture]
public class InMemoryDynamicConfigTests
{
    [Test]
    public void Properties_When_Set_Should_ReturnAssignedValues()
    {
        // Arrange
        var sut = new InMemoryDynamicConfig("RoundRobin", 5);

        // Act
        sut.Strategy = "LeastQueue";
        sut.HealthCheckIntervalSeconds = 12;

        // Assert
        Assert.That(sut.Strategy, Is.EqualTo("LeastQueue"));
        Assert.That(sut.HealthCheckIntervalSeconds, Is.EqualTo(12));
    }
}