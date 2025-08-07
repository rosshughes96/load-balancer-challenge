namespace LoadBalancerProject.Tests.Options
{
    using LoadBalancerProject.Options;
    using NUnit.Framework;

    [TestFixture]
    public class LoadBalancerOptionsTests
    {
        [Test]
        public void Constructor_Defaults_AreExpected()
        {
            // Arrange & Act
            var opts = new LoadBalancerOptions();

            // Assert
            Assert.That(opts.Strategy, Is.EqualTo("RoundRobin"));
            Assert.That(opts.HealthCheckIntervalSeconds, Is.EqualTo(5));
            Assert.That(opts.ListenPort, Is.EqualTo(6000));
            Assert.That(opts.Backends, Is.Empty);
        }
    }
}
