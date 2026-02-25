using DellarteDellaGuerra.Domain.SiegeEngines.Model;
using Xunit;

namespace DellarteDellaGuerra.Domain.Tests.SiegeEngines
{
    public class CannonPropertiesTests
    {
        [Fact]
        public void TestCannonPropertiesCreation()
        {
            // Arrange & Act
            var properties = new CannonProperties(
                "test_cannon",
                "Test Cannon",
                "test_sprite",
                "test_prefab",
                "test_projectile",
                "test_reload",
                "test_fire",
                1,
                0
            );

            // Assert
            Assert.Equal("test_cannon", properties.Id);
            Assert.Equal("Test Cannon", properties.DisplayName);
            Assert.Equal("test_sprite", properties.SpriteId);
            Assert.Equal("test_prefab", properties.MapPrefabName);
            Assert.Equal("test_projectile", properties.ProjectilePrefab);
            Assert.Equal("test_reload", properties.ReloadPrefab);
            Assert.Equal("test_fire", properties.FirePrefab);
            Assert.Equal(1, properties.MachineType);
            Assert.Equal(0, properties.ProjectileBoneIndex);
        }

        [Fact]
        public void TestCannonPropertiesEquality()
        {
            // Arrange
            var properties1 = new CannonProperties(
                "test_cannon", "Test", "sprite", "prefab", "proj", "reload", "fire", 1, 0
            );
            var properties2 = new CannonProperties(
                "test_cannon", "Test", "sprite", "prefab", "proj", "reload", "fire", 1, 0
            );
            var properties3 = new CannonProperties(
                "other_cannon", "Test", "sprite", "prefab", "proj", "reload", "fire", 1, 0
            );

            // Act & Assert
            Assert.Equal(properties1, properties2);
            Assert.NotEqual(properties1, properties3);
        }

        [Fact]
        public void TestCannonPropertiesDeconstruction()
        {
            // Arrange
            var properties = new CannonProperties(
                "test_cannon",
                "Test Cannon",
                "test_sprite",
                "test_prefab",
                "test_projectile",
                "test_reload",
                "test_fire",
                1,
                0
            );

            // Act
            var (id, displayName, spriteId, mapPrefabName, projectilePrefab, reloadPrefab, firePrefab, machineType, projectileBoneIndex) = properties;

            // Assert
            Assert.Equal("test_cannon", id);
            Assert.Equal("Test Cannon", displayName);
            Assert.Equal("test_sprite", spriteId);
            Assert.Equal("test_prefab", mapPrefabName);
            Assert.Equal("test_projectile", projectilePrefab);
            Assert.Equal("test_reload", reloadPrefab);
            Assert.Equal("test_fire", firePrefab);
            Assert.Equal(1, machineType);
            Assert.Equal(0, projectileBoneIndex);
        }

        [Fact]
        public void TestCannonPropertiesToString()
        {
            // Arrange
            var properties = new CannonProperties(
                "test_cannon",
                "Test Cannon",
                "test_sprite",
                "test_prefab",
                "test_projectile",
                "test_reload",
                "test_fire",
                1,
                0
            );

            // Act
            var toStringResult = properties.ToString();

            // Assert
            Assert.Contains("test_cannon", toStringResult);
            Assert.Contains("Test Cannon", toStringResult);
            Assert.Contains("test_sprite", toStringResult);
        }
    }
}