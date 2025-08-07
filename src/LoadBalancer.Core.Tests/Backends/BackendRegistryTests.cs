using LoadBalancerProject.Backends;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;


namespace LoadBalancerProject.Tests.Backends;

[TestFixture]
public class BackendRegistryTests
{
    [Test]
    public void Add_Remove_List_Are_Correct()
    {
        // Arrange
        var logger = Substitute.For<ILogger<BackendRegistry>>();
        var reg = new BackendRegistry(logger);
        var a = new Uri("tcp://127.0.0.1:5001");
        var b = new Uri("tcp://127.0.0.1:5002");

        // Act
        var addedA = reg.Add(a);
        var addedB = reg.Add(b);
        var addedADup = reg.Add(a);
        var list1 = reg.List();
        var removedA = reg.Remove(a);
        var removedAMissing = reg.Remove(a);
        var list2 = reg.List();

        // Assert
        Assert.That(addedA, Is.True);
        Assert.That(addedB, Is.True);
        Assert.That(addedADup, Is.False);
        Assert.That(list1, Has.Count.EqualTo(2));
        Assert.That(removedA, Is.True);
        Assert.That(removedAMissing, Is.False);
        Assert.That(list2, Has.Count.EqualTo(1));
        Assert.That(list2[0], Is.EqualTo(b));
    }
}