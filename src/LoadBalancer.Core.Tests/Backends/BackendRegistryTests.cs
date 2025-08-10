namespace LoadBalancerProject.Tests.Backends
{
    using LoadBalancerProject.Backends;
    using LoadBalancerProject.Tests.Common;
    using Microsoft.Extensions.Logging;
    using NSubstitute;
    using NUnit.Framework;
    using System;
    using System.Linq;

    [TestFixture]
    public class BackendRegistryTests
    {
        [Test]
        public void Add_NewBackend_ReturnsTrueAndLogsInformation()
        {
            // Arrange
            var logger = Substitute.For<ILogger<BackendRegistry>>();
            var sut = new BackendRegistry(logger);
            var backend = new Uri("tcp://localhost:1234");

            // Act
            var added = sut.Add(backend);

            // Assert
            Assert.That(added, Is.True);
            logger.AssertLogContains(LogLevel.Information, "Added backend");
        }

        [Test]
        public void Add_DuplicateBackend_ReturnsFalseAndLogsDebug()
        {
            // Arrange
            var logger = Substitute.For<ILogger<BackendRegistry>>();
            var sut = new BackendRegistry(logger);
            var backend = new Uri("tcp://localhost:1234");
            Assert.That(sut.Add(backend), Is.True);

            // Act
            var addedAgain = sut.Add(new Uri("tcp://LOCALHOST:1234"));

            // Assert
            Assert.That(addedAgain, Is.False);
            logger.AssertLogContains(LogLevel.Debug, "already exists");
        }

        [Test]
        public void Remove_ExistingBackend_ReturnsTrueAndLogsInformation()
        {
            // Arrange
            var logger = Substitute.For<ILogger<BackendRegistry>>();
            var sut = new BackendRegistry(logger);
            var backend = new Uri("tcp://localhost:1234");
            sut.Add(backend);

            // Act
            var removed = sut.Remove(new Uri("tcp://LOCALHOST:1234"));

            // Assert
            Assert.That(removed, Is.True);
            logger.AssertLogContains(LogLevel.Information, "Removed backend");
        }

        [Test]
        public void Remove_NotExistingBackend_ReturnsFalseAndLogsDebug()
        {
            // Arrange
            var logger = Substitute.For<ILogger<BackendRegistry>>();
            var sut = new BackendRegistry(logger);
            var backend = new Uri("tcp://localhost:9999");

            // Act
            var removed = sut.Remove(backend);

            // Assert
            Assert.That(removed, Is.False);
            logger.AssertLogContains(LogLevel.Debug, "not found");
        }

        [Test]
        public void Contains_WhenPresent_ReturnsTrueAndLogsDebug()
        {
            // Arrange
            var logger = Substitute.For<ILogger<BackendRegistry>>();
            var sut = new BackendRegistry(logger);
            sut.Add(new Uri("tcp://host:1"));

            // Act
            var exists = sut.Contains(new Uri("tcp://HOST:1"));

            // Assert
            Assert.That(exists, Is.True);
            logger.AssertLogContains(LogLevel.Debug, "Contains check");
            logger.AssertLogContains(LogLevel.Debug, "True");
        }

        [Test]
        public void Contains_WhenAbsent_ReturnsFalseAndLogsDebug()
        {
            // Arrange
            var logger = Substitute.For<ILogger<BackendRegistry>>();
            var sut = new BackendRegistry(logger);

            // Act
            var exists = sut.Contains(new Uri("tcp://host:1"));

            // Assert
            Assert.That(exists, Is.False);
            logger.AssertLogContains(LogLevel.Debug, "Contains check");
            logger.AssertLogContains(LogLevel.Debug, "False");
        }

        [Test]
        public void List_WhenMultiple_ReturnsSortedAndLogsCount()
        {
            // Arrange
            var logger = Substitute.For<ILogger<BackendRegistry>>();
            var sut = new BackendRegistry(logger);
            sut.Add(new Uri("tcp://b:2"));
            sut.Add(new Uri("tcp://a:1"));
            sut.Add(new Uri("tcp://c:3"));

            // Act
            var list = sut.List();

            // Assert
            Assert.That(list.Select(u => u.ToString()), Is.EqualTo(new[]
            {
                "tcp://a:1/",
                "tcp://b:2/",
                "tcp://c:3/"
            }));
            logger.AssertLogContains(LogLevel.Debug, "Listed 3 backends");
        }

        [Test]
        public void List_WhenEmpty_ReturnsEmptyAndLogsZeroCount()
        {
            // Arrange
            var logger = Substitute.For<ILogger<BackendRegistry>>();
            var sut = new BackendRegistry(logger);

            // Act
            var list = sut.List();

            // Assert
            Assert.That(list, Is.Empty);
            logger.AssertLogContains(LogLevel.Debug, "Listed 0 backends");
        }

        // Removed tests after the removal of the redundant SetAll method

        /*[Test]
        public void SetAll_ReplacesExisting_OnlyNewRemainAndLogsCount()
        {
            // Arrange
            var logger = Substitute.For<ILogger<BackendRegistry>>();
            var sut = new BackendRegistry(logger);
            sut.Add(new Uri("tcp://old1:1"));
            sut.Add(new Uri("tcp://old2:2"));

            // Act
            sut.SetAll(new[] { new Uri("tcp://new:3") });

            // Assert
            Assert.That(sut.List().Select(u => u.ToString()), Is.EqualTo(new[] { "tcp://new:3/" }));
            logger.AssertLogContains(LogLevel.Information, "Set all backends");
            logger.AssertLogContains(LogLevel.Information, "Count 1");
        }*/

        /*[Test]
        public void SetAll_WithDuplicates_CountReflectsUniqueAfterNormalization()
        {
            // Arrange
            var logger = Substitute.For<ILogger<BackendRegistry>>();
            var sut = new BackendRegistry(logger);
            var inputs = new[]
            {
                new Uri("tcp://Dupe:1"),
                new Uri("tcp://dupe:1"),
                new Uri("tcp://other:2")
            };

            // Act
            sut.SetAll(inputs);

            // Assert
            var list = sut.List();
            Assert.That(list.Select(u => u.ToString()), Is.EquivalentTo(new[] { "tcp://dupe:1/", "tcp://other:2/" }));
            logger.AssertLogContains(LogLevel.Information, "Count 2");
        }*/

        [Test]
        public void CaseNormalisation_MixedCaseUris_TreatedAsSame()
        {
            // Arrange
            var logger = Substitute.For<ILogger<BackendRegistry>>();
            var sut = new BackendRegistry(logger);
            var upper = new Uri("tcp://HOST:1111");
            var lower = new Uri("tcp://host:1111");

            // Act
            var firstAdd = sut.Add(upper);
            var containsLower = sut.Contains(lower);
            var removedLower = sut.Remove(lower);

            // Assert
            Assert.That(firstAdd, Is.True);
            Assert.That(containsLower, Is.True);
            Assert.That(removedLower, Is.True);
        }

        [Test]
        public void Add_NullBackend_Throws()
        {
            // Arrange
            var logger = Substitute.For<ILogger<BackendRegistry>>();
            var sut = new BackendRegistry(logger);

            // Act & Assert
            Assert.Throws<NullReferenceException>(() => sut.Add(null!));
        }

        [Test]
        public void Remove_NullBackend_Throws()
        {
            // Arrange
            var logger = Substitute.For<ILogger<BackendRegistry>>();
            var sut = new BackendRegistry(logger);

            // Act & Assert
            Assert.Throws<NullReferenceException>(() => sut.Remove(null!));
        }

        [Test]
        public void Contains_NullBackend_Throws()
        {
            // Arrange
            var logger = Substitute.For<ILogger<BackendRegistry>>();
            var sut = new BackendRegistry(logger);

            // Act & Assert
            Assert.Throws<NullReferenceException>(() => sut.Contains(null!));
        }
    }
}
