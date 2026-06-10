using DellarteDellaGuerra.Titles.Api;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Localization;

namespace DellarteDellaGuerra.Titles.Api.GameModels
{
    // Extends clan politics so that higher-ranked lords accumulate more influence per day
    // and pay less to override decisions, creating a meaningful feudal power hierarchy.
    public class DadgClanPoliticsModel : DefaultClanPoliticsModel
    {
        public override ExplainedNumber CalculateInfluenceChange(Clan clan, bool includeDescriptions = false)
        {
            var result = base.CalculateInfluenceChange(clan, includeDescriptions);
            if (!FeudalServices.IsInitialised || clan is null) return result;

            float tierBonus = FeudalServices.ComputeInfluenceTierBonus?.Execute(clan.StringId) ?? 0f;
            if (tierBonus > 0f)
                result.Add(tierBonus, new TextObject("Feudal Title Bonus"));

            return result;
        }

        public override int GetInfluenceRequiredToOverrideKingdomDecision(
            DecisionOutcome popularOption,
            DecisionOutcome overridingOption,
            KingdomDecision decision)
        {
            int baseCost = base.GetInfluenceRequiredToOverrideKingdomDecision(
                popularOption, overridingOption, decision);
            if (!FeudalServices.IsInitialised) return baseCost;

            float proposerTier = FeudalServices.ComputeInfluenceTierBonus?.Execute(
                decision.ProposerClan?.StringId ?? string.Empty) ?? 1f;
            float multiplier = proposerTier > 2f ? 0.7f : proposerTier < 0.5f ? 1.5f : 1f;

            return (int)(baseCost * multiplier);
        }
    }
}
