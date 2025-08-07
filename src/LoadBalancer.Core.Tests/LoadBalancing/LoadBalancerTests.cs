using LoadBalancerProject.Health;
using LoadBalancerProject.LoadBalancing;
using LoadBalancerProject.LoadBalancing.Strategies;
using LoadBalancerProject.Strategies;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;


namespace LoadBalancerProject.Tests.LoadBalancing;

[TestFixture]
public class LoadBalancerTests
{
    [Test]
    public void SelectBackend_Uses_Strategy_On_Healthy_List()
    {
        // Arrange
        var logger = Substitute.For<ILogger<LoadBalancer>>();
        var hc = Substitute.For<IHealthChecker>();
        var healthy = new List<Uri> { new("tcp://127.0.0.1:5001"), new("tcp://127.0.0.1:5002") };
        hc.GetHealthyBackends().Returns(healthy);

        var strategy = Substitute.For<IStrategyProvider>();
        strategy.Current.SelectBackend(healthy).Returns(healthy[1]); // Simulate strategy selecting second backend

        var lb = new LoadBalancer(logger, hc, strategy);

        // Act
        var selected = lb.SelectBackend();

        // Assert
        Assert.That(selected, Is.EqualTo(healthy[1]));
    }

    [Test]
    public void SelectBackend_Throws_When_No_Healthy()
    {
        // Arrange
        var logger = Substitute.For<ILogger<LoadBalancer>>();
        var hc = Substitute.For<IHealthChecker>();
        hc.GetHealthyBackends().Returns(new List<Uri>());
        var strategy = Substitute.For<IStrategyProvider>();
        var lb = new LoadBalancer(logger, hc, strategy);

        // Act/Assert
        Assert.That(() => lb.SelectBackend(), Throws.InvalidOperationException);
    }
}