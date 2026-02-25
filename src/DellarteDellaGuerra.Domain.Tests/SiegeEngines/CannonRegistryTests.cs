using DellarteDellaGuerra.Domain.SiegeEngines;
using DellarteDellaGuerra.Domain.SiegeEngines.Model;

namespace DellarteDellaGuerra.Domain.Tests.SiegeEngines;

public class CannonRegistryTests
{
    [Fact]
    public void TestCannonRegistryRegistrationAndRetrieval()
    {
        // Arrange
        var registry = new CannonRegistry();
        var cannonType = new TestCannonType("test_cannon");
        var factory = new TestCannonFactory();

        // Act
        registry.RegisterCannonType(cannonType, factory);

        // Assert
        var retrievedType = registry.GetCannonType("test_cannon");
        var retrievedFactory = registry.GetFactory("test_cannon");

        Assert.NotNull(retrievedType);
        Assert.NotNull(retrievedFactory);
        Assert.Equal("test_cannon", retrievedType.Id);
        Assert.Equal(cannonType, retrievedType);
        Assert.Equal(factory, retrievedFactory);
    }

    [Fact]
    public void TestCannonRegistryGetAllCannonTypes()
    {
        // Arrange
        var registry = new CannonRegistry();
        var cannonType1 = new TestCannonType("cannon1");
        var cannonType2 = new TestCannonType("cannon2");
        var factory1 = new TestCannonFactory();
        var factory2 = new TestCannonFactory();

        // Act
        registry.RegisterCannonType(cannonType1, factory1);
        registry.RegisterCannonType(cannonType2, factory2);

        var allTypes = registry.GetAllCannonTypes().ToList();

        // Assert
        Assert.Equal(2, allTypes.Count);
        Assert.Contains(cannonType1, allTypes);
        Assert.Contains(cannonType2, allTypes);
    }

    [Fact]
    public void TestCannonRegistryGetNonExistentType()
    {
        // Arrange
        var registry = new CannonRegistry();

        // Act
        var retrievedType = registry.GetCannonType("non_existent");
        var retrievedFactory = registry.GetFactory("non_existent");

        // Assert
        Assert.Null(retrievedType);
        Assert.Null(retrievedFactory);
    }

    private class TestCannonType : ICannonType
    {
        public string Id { get; }
        public string DisplayName => "Test Cannon";
        public string SpriteId => "test_sprite";
        public string MapPrefabName => "test_prefab";
        public string ProjectilePrefab => "test_projectile";
        public string ReloadPrefab => "test_reload";
        public string FirePrefab => "test_fire";
        public int MachineType => 1;
        public int ProjectileBoneIndex => 0;

        public TestCannonType(string id)
        {
            Id = id;
        }
    }

    private class TestCannonFactory
    {
        // Simple test factory that doesn't depend on Bannerlord types
    }
}