using DellarteDellaGuerra.Infrastructure.Configuration.Models;
using DellarteDellaGuerra.Infrastructure.Configuration.Providers;
using TaleWorlds.CampaignSystem;

namespace DellarteDellaGuerra.Infrastructure.Campaign;

public class FemalePartyLeaderRestrictionCampaignBehavior : CampaignBehaviorBase
{
    private readonly IConfigurationProvider<DadgConfig> _configProvider;

    public FemalePartyLeaderRestrictionCampaignBehavior(IConfigurationProvider<DadgConfig> configProvider)
    {
        _configProvider = configProvider;
    }

    public override void RegisterEvents() =>
        CampaignEvents.CanHeroLeadPartyEvent.AddNonSerializedListener(this, CanHeroLeadParty);

    public override void SyncData(IDataStore dataStore) { }

    private void CanHeroLeadParty(Hero hero, ref bool result)
    {
        if (hero.IsFemale && _configProvider.Config?.EnableFemalePartyLeaders != true)
        {
            result = false;
        }
    }
}
