using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using LoadBalancerProject.Backends;
using LoadBalancerProject.Configuration;
using LoadBalancerProject.Health;
using LoadBalancerProject.LoadBalancing;
using LoadBalancerProject.LoadBalancing.Strategies;
using LoadBalancerProject.Metrics;
using LoadBalancerProject.Queue;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;


namespace LoadBalancerProject.Tests.Backends;

[Category("Unit")]
[TestFixture]
public class BackendRegistryTests
{
    [Test]
    public void Add_When_NewUri_Should_AddAndReturnTrue()
    {
        // Arrange
        var logger = Substitute.For<ILogger<BackendRegistry>>();
        var sut = new BackendRegistry(logger);
        var a = new Uri("tcp://127.0.0.1:5001");

        // Act
        var added = sut.Add(a);
        var list = sut.List();

        // Assert
        Assert.That(added, Is.True);
        Assert.That(list, Has.Count.EqualTo(1));
        Assert.That(list[0], Is.EqualTo(a));
    }

    [Test]
    public void Add_When_Duplicate_Should_ReturnFalse_AndNotDuplicate()
    {
        // Arrange
        var logger = Substitute.For<ILogger<BackendRegistry>>();
        var sut = new BackendRegistry(logger);
        var a = new Uri("tcp://127.0.0.1:5001");
        sut.Add(a);

        // Act
        var addedAgain = sut.Add(a);
        var list = sut.List();

        // Assert
        Assert.That(addedAgain, Is.False);
        Assert.That(list, Has.Count.EqualTo(1));
    }

    [Test]
    public void Remove_When_Present_Should_RemoveAndReturnTrue()
    {
        // Arrange
        var logger = Substitute.For<ILogger<BackendRegistry>>();
        var sut = new BackendRegistry(logger);
        var a = new Uri("tcp://127.0.0.1:5001");
        sut.Add(a);

        // Act
        var removed = sut.Remove(a);
        var list = sut.List();

        // Assert
        Assert.That(removed, Is.True);
        Assert.That(list, Is.Empty);
    }

    [Test]
    public void Remove_When_NotPresent_Should_ReturnFalse()
    {
        // Arrange
        var logger = Substitute.For<ILogger<BackendRegistry>>();
        var sut = new BackendRegistry(logger);
        var a = new Uri("tcp://127.0.0.1:5001");

        // Act
        var removed = sut.Remove(a);

        // Assert
        Assert.That(removed, Is.False);
    }
}