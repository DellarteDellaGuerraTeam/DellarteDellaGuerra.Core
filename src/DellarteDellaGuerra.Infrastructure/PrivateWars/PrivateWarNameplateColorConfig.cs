using DellarteDellaGuerra.Domain.PrivateWars;
using DellarteDellaGuerra.Infrastructure.Configuration.Models;
using DellarteDellaGuerra.Infrastructure.Configuration.Providers;

namespace DellarteDellaGuerra.Infrastructure.PrivateWars
{
    // Supplies the raw configured ARGB hex string from dadg.config.xml.  Returns null when the
    // config or element is absent — validation and defaulting are owned by the domain use case
    // (PrivateWarNameplateColorUseCase), not this adapter.
    public class PrivateWarNameplateColorConfig : IPrivateWarNameplateColorProvider
    {
        private readonly IConfigurationProvider<DadgConfig> _configProvider;

        public PrivateWarNameplateColorConfig(IConfigurationProvider<DadgConfig> configProvider)
        {
            _configProvider = configProvider;
        }

        public string? GetConfiguredPrivateWarEnemyColorArgb()
            => _configProvider.Config?.PrivateWarConfig?.PrivateWarEnemyNameplateColorArgb;

        public string? GetConfiguredPrivateWarAllyColorArgb()
            => _configProvider.Config?.PrivateWarConfig?.PrivateWarAllyNameplateColorArgb;
    }
}
