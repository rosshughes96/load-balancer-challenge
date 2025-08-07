namespace LoadBalancerProject.Tests.LoadBalancing.Strategies
{
    using LoadBalancerProject.LoadBalancing.Strategies;
    using LoadBalancerProject.Tests.Common;
    using Microsoft.Extensions.Logging;
    using NSubstitute;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;

    [TestFixture]
    public class RoundRobinStrategyTests
    {
        [Test]
        public void SelectBackend_NullBackends_ThrowsInvalidOperation()
        {
            // Arrange
            var logger = Substitute.For<ILogger<RoundRobinStrategy>>();
            var sut = new RoundRobinStrategy(logger);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => sut.SelectBackend(null!));
        }

        [Test]
        public void SelectBackend_EmptyList_ThrowsInvalidOperation()
        {
            // Arrange
            var logger = Substitute.For<ILogger<RoundRobinStrategy>>();
            var sut = new RoundRobinStrategy(logger);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => sut.SelectBackend(Array.Empty<Uri>()));
        }

        [Test]
        public void SelectBackend_MultipleCalls_CyclesInStableOrderAndLogs()
        {
            // Arrange
            var logger = Substitute.For<ILogger<RoundRobinStrategy>>();
            var sut = new RoundRobinStrategy(logger);
            var list = new List<Uri>
            {
                new Uri("tcp://a:1"),
                new Uri("tcp://b:2"),
                new Uri("tcp://c:3")
            };

            // Act
            var first = sut.SelectBackend(list);
            var second = sut.SelectBackend(list);
            var third = sut.SelectBackend(list);
            var fourth = sut.SelectBackend(list);

            // Assert
            Assert.That(first, Is.EqualTo(list[0]));
            Assert.That(second, Is.EqualTo(list[1]));
            Assert.That(third, Is.EqualTo(list[2]));
            Assert.That(fourth, Is.EqualTo(list[0]));
            logger.AssertLogContains(LogLevel.Debug, "Round robin selected backend");
            logger.AssertLogContains(LogLevel.Debug, "index");
        }
    }
}
