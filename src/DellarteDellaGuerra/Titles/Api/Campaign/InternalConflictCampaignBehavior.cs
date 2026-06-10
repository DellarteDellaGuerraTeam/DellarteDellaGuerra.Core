using System.Linq;
using DellarteDellaGuerra.Titles.Api.Election;
using TaleWorlds.CampaignSystem;

namespace DellarteDellaGuerra.Titles.Api.Campaign
{
    // Accumulates feudal tension daily for each clan's outstanding claims and triggers
    // a petition decision when the tension threshold is crossed.
    public class InternalConflictCampaignBehavior : CampaignBehaviorBase
    {
        private const float TensionPetitionThreshold = 10f;
        private const float DailyAccumulationRate = 1f;

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickClanEvent.AddNonSerializedListener(this, OnDailyTickClan);
        }

        public override void SyncData(IDataStore dataStore) { }

        private static void OnDailyTickClan(Clan clan)
        {
            if (!FeudalServices.IsInitialised) return;
            if (clan.Kingdom is null || clan.IsEliminated) return;

            var claims = FeudalServices.Claims!.GetClaimsFor(clan.StringId);
            foreach (var claim in claims)
            {
                var tension = FeudalServices.AccumulateTension!.Execute(clan.StringId, claim, DailyAccumulationRate);
                if (tension.Amount >= TensionPetitionThreshold)
                    TryTriggerPetition(clan, claim.TitleId);
            }
        }

        private static void TryTriggerPetition(Clan claimantClan, string titleId)
        {
            var structure = FeudalServices.Structure!;
            var titles = FeudalServices.Titles!;

            string? suzerainTitleId = structure.GetDeJureSuzerainTitleId(titleId);
            if (suzerainTitleId is null) return;

            var suzerainTitle = titles.GetTitle(suzerainTitleId);
            if (suzerainTitle?.HolderClanId is null) return;

            var suzerainClan = Clan.All.FirstOrDefault(c =>
                c.StringId == suzerainTitle.HolderClanId && !c.IsEliminated);
            if (suzerainClan is null || suzerainClan.Kingdom != claimantClan.Kingdom) return;

            bool petitionAlreadyPending = claimantClan.Kingdom.UnresolvedDecisions
                .OfType<FeudalPetitionDecision>()
                .Any(d => d.ProposerClan == claimantClan);
            if (petitionAlreadyPending) return;

            var petition = new FeudalPetitionDecision(claimantClan, suzerainClan, titleId);
            claimantClan.Kingdom.AddDecision(petition, false);
        }
    }
}
