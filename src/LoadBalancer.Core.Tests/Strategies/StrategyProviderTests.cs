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


namespace LoadBalancerProject.Tests.Strategies;

[Category("Unit")]
[TestFixture]
public class StrategyProviderTests
{
    [Test]
    public void Current_When_RoundRobin_Config_Should_BeRoundRobin()
    {
        // Arrange
        var cfg = Substitute.For<IDynamicConfig>();
        cfg.Strategy.Returns("RoundRobin");
        var rr = new RoundRobinStrategy();
        var lq = new LeastQueueStrategy(Substitute.For<IBackendQueueTracker>());
        var logger = Substitute.For<ILogger<StrategyProvider>>();

        var sut = new StrategyProvider(cfg, rr, lq, logger);

        // Act
        var current = sut.Current;

        // Assert
        Assert.That(current, Is.SameAs(rr));
    }

    [Test]
    public void Refresh_When_LeastQueue_Config_Should_SwitchToLeastQueue()
    {
        // Arrange
        var cfg = Substitute.For<IDynamicConfig>();
        cfg.Strategy.Returns("RoundRobin");
        var rr = new RoundRobinStrategy();
        var lq = new LeastQueueStrategy(Substitute.For<IBackendQueueTracker>());
        var logger = Substitute.For<ILogger<StrategyProvider>>();
        var sut = new StrategyProvider(cfg, rr, lq, logger);

        // Act
        cfg.Strategy.Returns("LeastQueue");
        sut.Refresh();

        // Assert
        Assert.That(sut.Current, Is.SameAs(lq));
    }
}