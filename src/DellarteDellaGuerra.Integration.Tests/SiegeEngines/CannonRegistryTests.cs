using System;
using System.Linq;
using DellarteDellaGuerra.Domain.SiegeEngines.Model;
using DellarteDellaGuerra.Infrastructure.SiegeEngines;
using DellarteDellaGuerra.Infrastructure.SiegeEngines.Port;
using DellarteDellaGuerra.Integration.SiegeEngines.Mission.Siege.Spawn;
using Xunit;

namespace DellarteDellaGuerra.Integration.Tests.SiegeEngines;

public class CannonRegistryTests
{
    [Fact]
    public void RegisterCannon_WithValidParameters_StoresCannon()
    {
        // Arrange
        var registry = new CannonRegistry();
        var cannon = CreateTestCannon("test_cannon");
        var factory = new FakeCannonFactory(typeof(TestGenericCannon));

        // Act
        registry.RegisterCannon(cannon, factory);

        // Assert
        var retrievedCannon = registry.GetCannon("test_cannon");
        Assert.NotNull(retrievedCannon);
        Assert.Equal("test_cannon", retrievedCannon!.Id);
        
        var retrievedFactory = registry.GetFactory("test_cannon");
        Assert.NotNull(retrievedFactory);
        Assert.Same(factory, retrievedFactory);
    }

    [Fact]
    public void GetCannon_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        var registry = new CannonRegistry();

        // Act
        var result = registry.GetCannon("non_existent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetCannonByScript_WithExistingScriptType_ReturnsCannon()
    {
        // Arrange
        var registry = new CannonRegistry();
        var cannon = CreateTestCannon("test_cannon");
        var scriptType = typeof(TestGenericCannon);
        var factory = new FakeCannonFactory(scriptType);
        registry.RegisterCannon(cannon, factory);

        // Act
        var result = registry.GetCannonByScript(scriptType);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test_cannon", result!.Id);
    }

    [Fact]
    public void GetCannonByScript_WithNonExistentScriptType_ReturnsNull()
    {
        // Arrange
        var registry = new CannonRegistry();

        // Act
        var result = registry.GetCannonByScript(typeof(string));

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetFactory_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        var registry = new CannonRegistry();

        // Act
        var result = registry.GetFactory("non_existent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetAllCannons_WithMultipleCannons_ReturnsAllCannons()
    {
        // Arrange
        var registry = new CannonRegistry();
        var cannon1 = CreateTestCannon("cannon1");
        var cannon2 = CreateTestCannon("cannon2");
        var factory1 = new FakeCannonFactory(typeof(TestGenericCannon));
        var factory2 = new FakeCannonFactory(typeof(TestGenericCannon));
        
        registry.RegisterCannon(cannon1, factory1);
        registry.RegisterCannon(cannon2, factory2);

        // Act
        var allCannons = registry.GetAllCannons().ToList();

        // Assert
        Assert.Equal(2, allCannons.Count);
        Assert.Contains(allCannons, c => c.Id == "cannon1");
        Assert.Contains(allCannons, c => c.Id == "cannon2");
    }

    private static Cannon CreateTestCannon(string id)
    {
        return new Cannon(
            id,
            "Test Cannon",
            "test_sprite",
            "test_marker",
            "test_selection",
            "test_prefab",
            "test_projectile",
            "test_reload",
            "test_fire",
            1,
            0
        );
    }

    private class TestGenericCannon : GenericCannon
    {
        // Test class that inherits from GenericCannon
    }

    private class FakeCannonFactory : ICannonFactory
    {
        public Type CannonScriptType { get; }

        public FakeCannonFactory(Type cannonScriptType)
        {
            CannonScriptType = cannonScriptType;
        }

        public Infrastructure.SiegeEngines.SpawnableArtilleryRangedSiegeWeapon CreateCannon()
        {
            throw new NotImplementedException();
        }

        public void ConfigureSpawner(TaleWorlds.MountAndBlade.SpawnerEntityMissionHelper helper)
        {
            throw new NotImplementedException();
        }
    }
}