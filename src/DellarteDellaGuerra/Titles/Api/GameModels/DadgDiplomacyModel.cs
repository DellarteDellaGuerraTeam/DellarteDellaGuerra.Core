using DellarteDellaGuerra.Titles.Api;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;

namespace DellarteDellaGuerra.Titles.Api.GameModels
{
    // Extends vanilla diplomacy so army member influence scales by feudal tier.
    public class DadgDiplomacyModel : DefaultDiplomacyModel
    {
        public override float GetHourlyInfluenceAwardForBeingArmyMember(MobileParty mobileParty)
        {
            float baseAward = base.GetHourlyInfluenceAwardForBeingArmyMember(mobileParty);
            if (!FeudalServices.IsInitialised || mobileParty.LeaderHero?.Clan is null)
                return baseAward;

            float tierBonus = FeudalServices.ComputeInfluenceTierBonus?.Execute(
                mobileParty.LeaderHero.Clan.StringId) ?? 1f;
            return baseAward * tierBonus;
        }
    }
}
