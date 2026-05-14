using DellarteDellaGuerra.Domain.CharacterCreation.Ports;
using DellarteDellaGuerra.Infrastructure.Configuration.Models;
using DellarteDellaGuerra.Infrastructure.Configuration.Providers;

namespace DellarteDellaGuerra.Infrastructure.CharacterCreation.Providers
{
    public class AdvancedBannerBuilderConfig : IAdvancedBannerBuilderConfig
    {
        private readonly IConfigurationProvider<DadgConfig> _configProvider;

        public AdvancedBannerBuilderConfig(IConfigurationProvider<DadgConfig> configProvider)
        {
            _configProvider = configProvider;
        }

        public bool IsEnabled()
        {
            return _configProvider.Config?.EnableAdvancedBannerBuilder ?? true;
        }
    }
}
