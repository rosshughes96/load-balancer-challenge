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
using LoadBalancerProject.Strategies;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;


namespace LoadBalancerProject.Tests.LoadBalancing;

[Category("Unit")]
[TestFixture]
public class LoadBalancerTests
{
    [Test]
    public void SelectBackend_When_HealthyBackends_Should_UseStrategyResult()
    {
        // Arrange
        var logger = Substitute.For<ILogger<LoadBalancer>>();
        var hc = Substitute.For<IHealthChecker>();
        var healthy = new List<Uri> { new("tcp://127.0.0.1:5001"), new("tcp://127.0.0.1:5002") };
        hc.GetHealthyBackends().Returns(healthy);

        var strategy = Substitute.For<IStrategyProvider>();
        strategy.Current
            .SelectBackend(Arg.Is<IReadOnlyList<Uri>>(x => x.SequenceEqual(healthy)))
            .Returns(healthy[0]); // Simulate round-robin selecting the first backend

        var sut = new LoadBalancer(logger, hc, strategy);

        // Act
        var selected = sut.SelectBackend();

        // Assert
        Assert.That(selected, Is.EqualTo(healthy[0]));
    }

    [Test]
    public void SelectBackend_When_NoHealthy_Should_Throw()
    {
        // Arrange
        var logger = Substitute.For<ILogger<LoadBalancer>>();
        var hc = Substitute.For<IHealthChecker>();
        hc.GetHealthyBackends().Returns(new List<Uri>());
        var strategy = Substitute.For<IStrategyProvider>();

        var sut = new LoadBalancer(logger, hc, strategy);

        // Act / Assert
        Assert.That(() => sut.SelectBackend(), Throws.InvalidOperationException);
    }
}