using LoadBalancerProject.Backends;
using LoadBalancerProject.Configuration;
using LoadBalancerProject.Health;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;


namespace LoadBalancerProject.Tests.Health;

[TestFixture]
public class DynamicHealthCheckerSmokeTests
{
    [Test]
    public void Initially_Empty_When_No_Backends()
    {
        // Arrange
        var logger = Substitute.For<ILogger<DynamicHealthChecker>>();
        var reg = Substitute.For<IBackendRegistry>();
        reg.List().Returns(new List<Uri>());
        var cfg = Substitute.For<IDynamicConfig>();
        cfg.HealthCheckIntervalSeconds.Returns(1);
        var hc = new DynamicHealthChecker(logger, reg, cfg);

        // Act
        var list = hc.GetHealthyBackends();

        // Assert
        Assert.That(list, Is.Empty);
    }
}