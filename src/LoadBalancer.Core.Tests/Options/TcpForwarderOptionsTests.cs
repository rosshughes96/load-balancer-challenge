namespace LoadBalancerProject.Tests.Options
{
    using LoadBalancerProject.Options;
    using NUnit.Framework;

    [TestFixture]
    public class TcpForwarderOptionsTests
    {
        [Test]
        public void Constructor_Defaults_AreExpected()
        {
            // Arrange & Act
            var opts = new TcpForwarderOptions();

            // Assert
            Assert.That(opts.MaxConcurrentConnections, Is.EqualTo(100));
            Assert.That(opts.IdleTimeoutSeconds, Is.EqualTo(15));
            Assert.That(opts.MaxLifetimeSeconds, Is.EqualTo(300));
            Assert.That(opts.BufferSize, Is.EqualTo(8192));
        }
    }
}
