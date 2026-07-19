using DellarteDellaGuerra.Domain.Church;
using DellarteDellaGuerra.Domain.Church.Port;
using DellarteDellaGuerra.Infrastructure.Configuration.Models;
using DellarteDellaGuerra.Infrastructure.Configuration.Providers;

namespace DellarteDellaGuerra.Infrastructure.Church
{
    public class ChurchSettingsConfig : IChurchSettingsProvider
    {
        private readonly IConfigurationProvider<DadgConfig> _configProvider;

        public ChurchSettingsConfig(IConfigurationProvider<DadgConfig> configProvider)
        {
            _configProvider = configProvider;
        }

        public ChurchSettings GetSettings()
        {
            var config = _configProvider.Config?.ChurchConfig ?? new ChurchConfig();
            return new ChurchSettings(
                config.DonationCost,
                config.DonationRelation,
                config.MassRelation,
                config.MassMorale,
                config.SacrilegeRelationLocal,
                config.SacrilegeRelationOthers,
                config.WeeklyTithePower,
                config.DonationPower,
                config.BlessingMorale,
                config.MaxPilgrimParties,
                config.PilgrimProtectionRelation);
        }
    }
}
