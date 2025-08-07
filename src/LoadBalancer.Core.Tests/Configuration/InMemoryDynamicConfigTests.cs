using LoadBalancerProject.Configuration;
using NUnit.Framework;


namespace LoadBalancerProject.Tests.Configuration;

[TestFixture]
public class InMemoryDynamicConfigTests
{
    [Test]
    public void Properties_Are_Assignable_And_Retained()
    {
        // Arrange
        var cfg = new InMemoryDynamicConfig("RoundRobin", 5);

        // Act
        cfg.Strategy = "LeastQueue";
        cfg.HealthCheckIntervalSeconds = 15;

        // Assert
        Assert.That(cfg.Strategy, Is.EqualTo("LeastQueue"));
        Assert.That(cfg.HealthCheckIntervalSeconds, Is.EqualTo(15));
    }
}