using DellarteDellaGuerra.Domain.Combat.Port;
using DellarteDellaGuerra.Infrastructure.Configuration.Models;
using DellarteDellaGuerra.Infrastructure.Configuration.Providers;

namespace DellarteDellaGuerra.Infrastructure.Combat
{
    public class CombatFeatureToggleConfig : ICombatFeatureToggle
    {
        private const bool DefaultEnabled = true;
        private readonly IConfigurationProvider<DadgConfig> _configProvider;

        public CombatFeatureToggleConfig(IConfigurationProvider<DadgConfig> configProvider)
        {
            _configProvider = configProvider;
        }

        public bool IsCombatOverhaulEnabled =>
            _configProvider.Config?.CombatConfig?.EnableCombatOverhaul ?? DefaultEnabled;
    }
}
