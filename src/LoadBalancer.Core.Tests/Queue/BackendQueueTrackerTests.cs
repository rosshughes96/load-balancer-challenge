namespace LoadBalancerProject.Tests.Queue
{
    using LoadBalancerProject.Queue;
    using LoadBalancerProject.Tests.Common;
    using Microsoft.Extensions.Logging;
    using NSubstitute;
    using NUnit.Framework;
    using System;
    using System.Threading.Tasks;

    [TestFixture]
    public class BackendQueueTrackerTests
    {
        [Test]
        public void Increment_NewBackend_SetsTo1_AndLogsDebug()
        {
            // Arrange
            var logger = Substitute.For<ILogger<BackendQueueTracker>>();
            var sut = new BackendQueueTracker(logger);
            var backend = new Uri("tcp://127.0.0.1:9001");

            // Act
            sut.Increment(backend);

            // Assert
            Assert.That(sut.GetQueueLength(backend), Is.EqualTo(1));
            logger.AssertLogContains(LogLevel.Debug, "Incremented queue");
            logger.AssertLogContains(LogLevel.Debug, "1");
        }

        [Test]
        public void Increment_Twice_SetsTo2_AndLogs()
        {
            // Arrange
            var logger = Substitute.For<ILogger<BackendQueueTracker>>();
            var sut = new BackendQueueTracker(logger);
            var backend = new Uri("tcp://127.0.0.1:9002");

            // Act
            sut.Increment(backend);
            sut.Increment(backend);

            // Assert
            Assert.That(sut.GetQueueLength(backend), Is.EqualTo(2));
            logger.AssertLogContains(LogLevel.Debug, "Incremented queue");
        }

        [Test]
        public void Decrement_WhenZero_StaysZero_AndLogsDebug()
        {
            // Arrange
            var logger = Substitute.For<ILogger<BackendQueueTracker>>();
            var sut = new BackendQueueTracker(logger);
            var backend = new Uri("tcp://127.0.0.1:9003");

            // Act
            sut.Decrement(backend);

            // Assert
            Assert.That(sut.GetQueueLength(backend), Is.EqualTo(0));
            logger.AssertLogContains(LogLevel.Debug, "Decremented queue");
            logger.AssertLogContains(LogLevel.Debug, "0");
        }

        [Test]
        public void Decrement_AfterIncrement_ReturnsToZero_AndLogs()
        {
            // Arrange
            var logger = Substitute.For<ILogger<BackendQueueTracker>>();
            var sut = new BackendQueueTracker(logger);
            var backend = new Uri("tcp://127.0.0.1:9004");

            // Act
            sut.Increment(backend);
            sut.Decrement(backend);

            // Assert
            Assert.That(sut.GetQueueLength(backend), Is.EqualTo(0));
            logger.AssertLogContains(LogLevel.Debug, "Decremented queue");
        }

        [Test]
        public void GetQueueLength_UnknownBackend_ReturnsZero()
        {
            // Arrange
            var logger = Substitute.For<ILogger<BackendQueueTracker>>();
            var sut = new BackendQueueTracker(logger);
            var backend = new Uri("tcp://127.0.0.1:9005");

            // Act
            var length = sut.GetQueueLength(backend);

            // Assert
            Assert.That(length, Is.EqualTo(0));
        }

        [Test]
        public void Increment_ParallelMany_CounterAccurate()
        {
            // Arrange
            var logger = Substitute.For<ILogger<BackendQueueTracker>>();
            var sut = new BackendQueueTracker(logger);
            var backend = new Uri("tcp://127.0.0.1:9006");
            var n = 200;

            // Act
            Parallel.For(0, n, _ => sut.Increment(backend));

            // Assert
            Assert.That(sut.GetQueueLength(backend), Is.EqualTo(n));
        }
    }
}
