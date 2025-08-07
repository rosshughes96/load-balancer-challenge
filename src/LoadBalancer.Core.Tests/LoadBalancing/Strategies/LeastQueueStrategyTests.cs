namespace LoadBalancerProject.Tests.LoadBalancing.Strategies
{
    using LoadBalancerProject.LoadBalancing.Strategies;
    using LoadBalancerProject.Queue;
    using LoadBalancerProject.Tests.Common;
    using Microsoft.Extensions.Logging;
    using NSubstitute;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;

    [TestFixture]
    public class LeastQueueStrategyTests
    {
        [Test]
        public void SelectBackend_Null_ThrowsInvalidOperation()
        {
            // Arrange
            var queue = Substitute.For<IBackendQueueTracker>();
            var logger = Substitute.For<ILogger<LeastQueueStrategy>>();
            var sut = new LeastQueueStrategy(queue, logger);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => sut.SelectBackend(null!));
        }

        [Test]
        public void SelectBackend_Empty_ThrowsInvalidOperation()
        {
            // Arrange
            var queue = Substitute.For<IBackendQueueTracker>();
            var logger = Substitute.For<ILogger<LeastQueueStrategy>>();
            var sut = new LeastQueueStrategy(queue, logger);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => sut.SelectBackend(Array.Empty<Uri>()));
        }

        [Test]
        public void SelectBackend_DifferentQueues_PicksSmallestAndLogs()
        {
            // Arrange
            var queue = Substitute.For<IBackendQueueTracker>();
            var logger = Substitute.For<ILogger<LeastQueueStrategy>>();
            var sut = new LeastQueueStrategy(queue, logger);
            var a = new Uri("tcp://a:1");
            var b = new Uri("tcp://b:2");
            var c = new Uri("tcp://c:3");
            var list = new List<Uri> { a, b, c };

            queue.GetQueueLength(a).Returns(5);
            queue.GetQueueLength(b).Returns(2);
            queue.GetQueueLength(c).Returns(3);

            // Act
            var selected = sut.SelectBackend(list);

            // Assert
            Assert.That(selected, Is.EqualTo(b));
            queue.Received(1).GetQueueLength(a);
            queue.Received(1).GetQueueLength(b);
            queue.Received(1).GetQueueLength(c);
            logger.AssertLogContains(LogLevel.Debug, "Selected backend");
            logger.AssertLogContains(LogLevel.Debug, "queue length");
        }

        [Test]
        public void SelectBackend_TieOnQueueLength_PicksFirstEncountered()
        {
            // Arrange
            var queue = Substitute.For<IBackendQueueTracker>();
            var logger = Substitute.For<ILogger<LeastQueueStrategy>>();
            var sut = new LeastQueueStrategy(queue, logger);
            var a = new Uri("tcp://a:1");
            var b = new Uri("tcp://b:2");
            var list = new List<Uri> { a, b };

            queue.GetQueueLength(a).Returns(1);
            queue.GetQueueLength(b).Returns(1);

            // Act
            var selected = sut.SelectBackend(list);

            // Assert
            Assert.That(selected, Is.EqualTo(a), "When equal, should keep the first as best");
        }
    }
}
