using DellarteDellaGuerra.Titles.Api.Election;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Election;

namespace DellarteDellaGuerra.Titles.Api.Campaign
{
    // Intercepts every SettlementClaimantDecision the game creates and replaces it with
    // FeudalSettlementClaimantDecision so that the chooser and support weights reflect the
    // feudal hierarchy rather than vanilla rules.
    public class FeudalTitleSwapBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.KingdomDecisionAdded.AddNonSerializedListener(this, OnKingdomDecisionAdded);
        }

        public override void SyncData(IDataStore dataStore) { }

        private static void OnKingdomDecisionAdded(KingdomDecision decision, bool isPlayerInvolved)
        {
            if (decision is FeudalSettlementClaimantDecision) return;
            if (decision is not SettlementClaimantDecision scd) return;

            var kingdom = decision.Kingdom;
            var feudalDecision = new FeudalSettlementClaimantDecision(
                scd.ProposerClan,
                scd.Settlement,
                null,
                scd.ClanToExclude);

            kingdom.RemoveDecision(scd);
            kingdom.AddDecision(feudalDecision, true);
        }
    }
}
