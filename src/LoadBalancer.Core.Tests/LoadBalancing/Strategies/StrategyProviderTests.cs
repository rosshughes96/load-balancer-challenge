namespace LoadBalancerProject.Tests.LoadBalancing.Strategies
{
    using LoadBalancerProject.Configuration;
    using LoadBalancerProject.LoadBalancing.Strategies;
    using LoadBalancerProject.Queue;
    using LoadBalancerProject.Strategies;
    using LoadBalancerProject.Tests.Common;
    using Microsoft.Extensions.Logging;
    using NSubstitute;
    using NUnit.Framework;

    [TestFixture]
    public class StrategyProviderTests
    {
        [Test]
        public void Constructor_InitialisesWithConfigValue_LogsInitialised()
        {
            // Arrange
            var config = Substitute.For<IDynamicConfig>();
            config.Strategy.Returns("LeastQueue");
            var rr = new RoundRobinStrategy(Substitute.For<ILogger<RoundRobinStrategy>>());
            var lq = new LeastQueueStrategy(Substitute.For<IBackendQueueTracker>(), Substitute.For<ILogger<LeastQueueStrategy>>());
            var logger = Substitute.For<ILogger<StrategyProvider>>();

            // Act
            var sut = new StrategyProvider(config, rr, lq, logger);

            // Assert
            Assert.That(sut.Current, Is.SameAs(lq));
            logger.AssertLogContains(LogLevel.Information, "initialised");
        }

        [Test]
        public void Current_UnknownStrategyName_FallsBackToRoundRobin()
        {
            // Arrange
            var config = Substitute.For<IDynamicConfig>();
            config.Strategy.Returns("Unknown123");
            var rr = new RoundRobinStrategy(Substitute.For<ILogger<RoundRobinStrategy>>());
            var lq = new LeastQueueStrategy(Substitute.For<IBackendQueueTracker>(), Substitute.For<ILogger<LeastQueueStrategy>>());
            var logger = Substitute.For<ILogger<StrategyProvider>>();

            // Act
            var sut = new StrategyProvider(config, rr, lq, logger);

            // Assert
            Assert.That(sut.Current, Is.SameAs(rr));
        }

        [Test]
        public void Refresh_WhenConfigChanges_UpdatesCurrentAndLogsChange()
        {
            // Arrange
            var config = Substitute.For<IDynamicConfig>();
            config.Strategy.Returns("RoundRobin");
            var rr = new RoundRobinStrategy(Substitute.For<ILogger<RoundRobinStrategy>>());
            var lq = new LeastQueueStrategy(Substitute.For<IBackendQueueTracker>(), Substitute.For<ILogger<LeastQueueStrategy>>());
            var logger = Substitute.For<ILogger<StrategyProvider>>();
            var sut = new StrategyProvider(config, rr, lq, logger);

            // Act
            config.Strategy.Returns("LeastQueue");
            sut.Refresh();

            // Assert
            Assert.That(sut.Current, Is.SameAs(lq));
            logger.AssertLogContains(LogLevel.Information, "Strategy changed to");
        }

        [Test]
        public void Resolve_CaseInsensitiveNames_SupportsLeastQueueIgnoringCase()
        {
            // Arrange
            var config = Substitute.For<IDynamicConfig>();
            config.Strategy.Returns("leastqueue");
            var rr = new RoundRobinStrategy(Substitute.For<ILogger<RoundRobinStrategy>>());
            var lq = new LeastQueueStrategy(Substitute.For<IBackendQueueTracker>(), Substitute.For<ILogger<LeastQueueStrategy>>());
            var logger = Substitute.For<ILogger<StrategyProvider>>();

            // Act
            var sut = new StrategyProvider(config, rr, lq, logger);

            // Assert
            Assert.That(sut.Current, Is.SameAs(lq));
        }
    }
}
