using LoadBalancerProject.Configuration;
using LoadBalancerProject.LoadBalancing.Strategies;
using LoadBalancerProject.Queue;
using LoadBalancerProject.Strategies;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;


namespace LoadBalancerProject.Tests.Strategies;

[TestFixture]
public class StrategyProviderTests
{
    [Test]
    public void Provider_Resolves_Concrete_Strategies_And_Refreshes()
    {
        // Arrange
        var cfg = Substitute.For<IDynamicConfig>();
        cfg.Strategy.Returns("RoundRobin");

        var rr = new RoundRobinStrategy();
        var lq = new LeastQueueStrategy(Substitute.For<IBackendQueueTracker>());
        var logger = Substitute.For<ILogger<StrategyProvider>>();

        var provider = new StrategyProvider(cfg, rr, lq, logger);

        // Assert initial
        Assert.That(provider.Current, Is.SameAs(rr));

        // Act - switch to LeastQueue
        cfg.Strategy.Returns("LeastQueue");
        provider.Refresh();

        // Assert
        Assert.That(provider.Current, Is.SameAs(lq));

        // Act - unknown -> default RoundRobin
        cfg.Strategy.Returns("??");
        provider.Refresh();

        // Assert
        Assert.That(provider.Current, Is.SameAs(rr));
    }
}