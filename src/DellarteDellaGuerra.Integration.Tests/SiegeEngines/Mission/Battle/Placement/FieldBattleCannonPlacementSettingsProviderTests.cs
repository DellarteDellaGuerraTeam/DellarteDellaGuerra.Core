using DellarteDellaGuerra.Infrastructure.Configuration.Models;
using DellarteDellaGuerra.Infrastructure.Configuration.Providers;
using DellarteDellaGuerra.Integration.SiegeEngines.Mission.Battle.Placement;

namespace DellarteDellaGuerra.Integration.Tests.SiegeEngines.Mission.Battle.Placement;

public class FieldBattleCannonPlacementSettingsProviderTests
{
    [Fact]
    public void IsEnabled_WhenConfigIsMissing_UsesDefaultTrue()
    {
        // Arrange
        var configProvider = new FakeConfigProvider();
        var provider = new FieldBattleCannonPlacementSettingsProvider(configProvider);

        // Act
        var enabled = provider.IsEnabled;

        // Assert
        Assert.True(enabled);
    }

    [Fact]
    public void PlacementLimit_WhenConfigIsMissing_UsesDefaultTwo()
    {
        // Arrange
        var configProvider = new FakeConfigProvider();
        var provider = new FieldBattleCannonPlacementSettingsProvider(configProvider);

        // Act
        var limit = provider.PlacementLimit;

        // Assert
        Assert.Equal(2, limit);
    }

    [Fact]
    public void PlacementLimit_WhenConfigIsNegative_IsClampedToZero()
    {
        // Arrange
        var configProvider = new FakeConfigProvider
        {
            Config = new DadgConfig
            {
                FieldBattleCannonPlacementLimit = -7
            }
        };
        var provider = new FieldBattleCannonPlacementSettingsProvider(configProvider);

        // Act
        var limit = provider.PlacementLimit;

        // Assert
        Assert.Equal(0, limit);
    }

    [Fact]
    public void IsEnabled_WhenConfiguredFalse_ReturnsFalse()
    {
        // Arrange
        var configProvider = new FakeConfigProvider
        {
            Config = new DadgConfig
            {
                EnableFieldBattleCannonPlacement = false
            }
        };
        var provider = new FieldBattleCannonPlacementSettingsProvider(configProvider);

        // Act
        var enabled = provider.IsEnabled;

        // Assert
        Assert.False(enabled);
    }

    private class FakeConfigProvider : IConfigurationProvider<DadgConfig>
    {
        public DadgConfig? Config { get; init; }
    }
}
