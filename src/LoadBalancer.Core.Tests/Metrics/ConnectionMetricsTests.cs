namespace LoadBalancerProject.Tests.Metrics
{
    using LoadBalancerProject.Metrics;
    using LoadBalancerProject.Tests.Common;
    using Microsoft.Extensions.Logging;
    using NSubstitute;
    using NUnit.Framework;
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    [TestFixture]
    public class ConnectionMetricsTests
    {
        [Test]
        public void OnConnectionStart_FirstCall_SetsActive1Total1_AndLogsDebug()
        {
            // Arrange
            var logger = Substitute.For<ILogger<ConnectionMetrics>>();
            var sut = new ConnectionMetrics(logger);
            var backend = new Uri("tcp://host:1001");

            // Act
            sut.OnConnectionStart(backend);
            var snap = sut.Snapshot();

            // Assert
            var entry = snap.Backends.Single();
            Assert.That(entry.Backend, Is.EqualTo(backend.ToString()));
            Assert.That(entry.Active, Is.EqualTo(1));
            Assert.That(entry.Total, Is.EqualTo(1));
            logger.AssertLogContains(LogLevel.Debug, "Connection started");
            logger.AssertLogContains(LogLevel.Debug, "Metrics snapshot created");
        }

        [Test]
        public void OnConnectionStart_MultipleBackends_TrackedSeparately()
        {
            // Arrange
            var logger = Substitute.For<ILogger<ConnectionMetrics>>();
            var sut = new ConnectionMetrics(logger);
            var a = new Uri("tcp://a:1");
            var b = new Uri("tcp://b:2");

            // Act
            sut.OnConnectionStart(a);
            sut.OnConnectionStart(a);
            sut.OnConnectionStart(b);

            var snap = sut.Snapshot();

            // Assert
            var aRow = snap.Backends.Single(x => x.Backend == a.ToString());
            var bRow = snap.Backends.Single(x => x.Backend == b.ToString());
            Assert.That(aRow.Active, Is.EqualTo(2));
            Assert.That(aRow.Total, Is.EqualTo(2));
            Assert.That(bRow.Active, Is.EqualTo(1));
            Assert.That(bRow.Total, Is.EqualTo(1));
            Assert.That(snap.ActiveAll, Is.EqualTo(3));
            Assert.That(snap.TotalAll, Is.EqualTo(3));
        }

        [Test]
        public void OnConnectionEnd_AfterStart_DecrementsActive_NotBelowZero_AndLogsDebug()
        {
            // Arrange
            var logger = Substitute.For<ILogger<ConnectionMetrics>>();
            var sut = new ConnectionMetrics(logger);
            var backend = new Uri("tcp://host:2002");

            // Act
            sut.OnConnectionStart(backend);
            sut.OnConnectionEnd(backend);
            var snap = sut.Snapshot();

            // Assert
            var entry = snap.Backends.Single();
            Assert.That(entry.Active, Is.EqualTo(0));
            Assert.That(entry.Total, Is.EqualTo(1), "Total should remain as the number of starts");
            logger.AssertLogContains(LogLevel.Debug, "Connection ended");
        }

        [Test]
        public void OnConnectionEnd_MoreEndsThanStarts_ClampsToZero_AndLogsWarning()
        {
            // Arrange
            var logger = Substitute.For<ILogger<ConnectionMetrics>>();
            var sut = new ConnectionMetrics(logger);
            var backend = new Uri("tcp://host:3003");

            // Act
            sut.OnConnectionStart(backend);
            sut.OnConnectionEnd(backend);
            sut.OnConnectionEnd(backend);
            var snap = sut.Snapshot();

            // Assert
            var entry = snap.Backends.Single();
            Assert.That(entry.Active, Is.EqualTo(0), "Active should be clamped to zero");
            Assert.That(entry.Total, Is.EqualTo(1));
            logger.AssertLogContains(LogLevel.Warning, "Active count went negative");
        }

        [Test]
        public void Snapshot_SortsByBackendAndAggregatesTotals_AndLogsDebug()
        {
            // Arrange
            var logger = Substitute.For<ILogger<ConnectionMetrics>>();
            var sut = new ConnectionMetrics(logger);
            var c = new Uri("tcp://c:3");
            var a = new Uri("tcp://a:1");
            var b = new Uri("tcp://b:2");

            // Act
            sut.OnConnectionStart(c);
            sut.OnConnectionStart(a);
            sut.OnConnectionStart(b);
            sut.OnConnectionStart(b);
            var snap = sut.Snapshot();

            // Assert
            Assert.That(snap.Backends.Select(x => x.Backend), Is.EqualTo(new[] { a, b, c }.Select(u => u.ToString()).OrderBy(s => s, StringComparer.Ordinal)));
            Assert.That(snap.ActiveAll, Is.EqualTo(4));
            Assert.That(snap.TotalAll, Is.EqualTo(4));
            logger.AssertLogContains(LogLevel.Debug, "Metrics snapshot created");
        }

        [Test]
        public void OnConnectionStart_ParallelIncrements_TotalsMatchCount()
        {
            // Arrange
            var logger = Substitute.For<ILogger<ConnectionMetrics>>();
            var sut = new ConnectionMetrics(logger);
            var backend = new Uri("tcp://parallel:9999");
            var n = 200;

            // Act
            Parallel.For(0, n, _ => sut.OnConnectionStart(backend));
            var snap = sut.Snapshot();

            // Assert
            var row = snap.Backends.Single();
            Assert.That(row.Active, Is.EqualTo(n));
            Assert.That(row.Total, Is.EqualTo(n));
            Assert.That(snap.ActiveAll, Is.EqualTo(n));
            Assert.That(snap.TotalAll, Is.EqualTo(n));
        }
    }
}
