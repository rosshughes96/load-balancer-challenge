namespace LoadBalancerProject.Tests.Draining
{
    using LoadBalancerProject.Draining;
    using NUnit.Framework;
    using System;

    [TestFixture]
    public class DrainControllerTests
    {
        [Test]
        public void BeginDrain_Then_IsDraining_ReturnsTrue()
        {
            // Arrange
            var sut = new DrainController();
            var backend = new Uri("tcp://host:1");

            // Act
            sut.BeginDrain(backend, TimeSpan.FromSeconds(5));

            // Assert
            Assert.That(sut.IsDraining(backend), Is.True);
        }

        [Test]
        public void Clear_AfterBeginDrain_IsDrainingReturnsFalse()
        {
            // Arrange
            var sut = new DrainController();
            var backend = new Uri("tcp://host:1");
            sut.BeginDrain(backend);

            // Act
            sut.Clear(backend);

            // Assert
            Assert.That(sut.IsDraining(backend), Is.False);
        }
    }
}
