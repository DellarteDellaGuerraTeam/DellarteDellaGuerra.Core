using DellarteDellaGuerra.Infrastructure.CharacterCreation.Providers;
using DellarteDellaGuerra.Infrastructure.Configuration.Models;
using DellarteDellaGuerra.Infrastructure.Configuration.Providers;

namespace DellarteDellaGuerra.Infrastructure.Tests.CharacterCreation;

public class AdvancedBannerBuilderConfigTests
{
    [Fact]
    public void IsEnabled_WhenConfigIsMissing_ReturnsTrueByDefault()
    {
        var provider = new FakeConfigProvider(null);
        var sut = new AdvancedBannerBuilderConfig(provider);

        var result = sut.IsEnabled();

        Assert.True(result);
    }

    [Fact]
    public void IsEnabled_WhenFlagIsTrue_ReturnsTrue()
    {
        var provider = new FakeConfigProvider(new DadgConfig
        {
            EnableAdvancedBannerBuilder = true
        });
        var sut = new AdvancedBannerBuilderConfig(provider);

        var result = sut.IsEnabled();

        Assert.True(result);
    }

    [Fact]
    public void IsEnabled_WhenFlagIsFalse_ReturnsFalse()
    {
        var provider = new FakeConfigProvider(new DadgConfig
        {
            EnableAdvancedBannerBuilder = false
        });
        var sut = new AdvancedBannerBuilderConfig(provider);

        var result = sut.IsEnabled();

        Assert.False(result);
    }

    [Fact]
    public void DadgConfig_EnableAdvancedBannerBuilder_DefaultsToTrue()
    {
        var config = new DadgConfig();

        Assert.True(config.EnableAdvancedBannerBuilder);
    }

    private class FakeConfigProvider(DadgConfig? config) : IConfigurationProvider<DadgConfig>
    {
        public DadgConfig? Config => config;
    }
}
