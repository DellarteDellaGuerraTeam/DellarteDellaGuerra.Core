using DellarteDellaGuerra.Titles.Api;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;

namespace DellarteDellaGuerra.Titles.Api.GameModels
{
    // Extends vanilla diplomacy with feudal tension: a clan's accumulated unfulfilled claims
    // boost its score to leave the kingdom, and army member influence scales by feudal tier.
    public class DadgDiplomacyModel : DefaultDiplomacyModel
    {
        public override float GetScoreOfClanToLeaveKingdom(Clan clan, Kingdom kingdom)
        {
            float baseScore = base.GetScoreOfClanToLeaveKingdom(clan, kingdom);
            if (!FeudalServices.IsInitialised) return baseScore;

            float tensionBonus = 0f;
            var claims = FeudalServices.Claims?.GetClaimsFor(clan.StringId);
            if (claims is not null)
            {
                foreach (var claim in claims)
                {
                    float tension = FeudalServices.Tensions?
                        .GetTension(clan.StringId, claim.TitleId)?.Amount ?? 0f;
                    tensionBonus += tension * 0.5f;
                }
            }

            return baseScore + tensionBonus;
        }

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
