using DellarteDellaGuerra.Domain.Tournament.Jousting.Port;
using DellarteDellaGuerra.Infrastructure.Configuration.Models;
using DellarteDellaGuerra.Infrastructure.Configuration.Providers;

namespace DellarteDellaGuerra.Infrastructure.Tournament.Jousting
{
    public class JoustingFeatureToggleConfig : IJoustingFeatureToggle
    {
        private const bool DefaultEnabled = true;
        private readonly IConfigurationProvider<DadgConfig> _configProvider;

        public JoustingFeatureToggleConfig(IConfigurationProvider<DadgConfig> configProvider)
        {
            _configProvider = configProvider;
        }

        public bool IsJoustingEnabled =>
            _configProvider.Config?.JoustingConfig?.EnableJousting ?? DefaultEnabled;
    }
}
