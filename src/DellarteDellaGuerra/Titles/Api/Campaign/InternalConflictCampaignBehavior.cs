using System.Linq;
using DellarteDellaGuerra.Titles.Api.Election;
using TaleWorlds.CampaignSystem;

namespace DellarteDellaGuerra.Titles.Api.Campaign
{
    // Accumulates feudal tension daily for each clan's outstanding claims and triggers
    // a petition decision when the tension threshold is crossed. Also proposes acts of
    // attainder for titles whose seat has been occupied by a fellow kingdom clan long
    // enough that the dispossessed holder's dignity is ripe for forfeiture.
    public class InternalConflictCampaignBehavior : CampaignBehaviorBase
    {
        private const float TensionPetitionThreshold = 10f;
        private const float DailyAccumulationRate = 1f;
        private const float AttainderGraceDays = 30f;

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickClanEvent.AddNonSerializedListener(this, OnDailyTickClan);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
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

        private static void OnDailyTick()
        {
            if (!FeudalServices.IsInitialised) return;

            float today = (float)CampaignTime.Now.ToDays;
            foreach (var title in FeudalServices.Titles!.GetAllTitles())
            {
                if (!title.IsContested || title.ContestedSinceDay is null) continue;
                if (today - title.ContestedSinceDay.Value < AttainderGraceDays) continue;

                // Attainder is internal kingdom law: only an occupant of the title's own
                // kingdom can be regularised. Cross-kingdom occupation waits for a peace.
                var occupant = FeudalTitleKingdoms.FindClan(title.OccupantClanId);
                var titleKingdom = FeudalTitleKingdoms.GetTitleKingdom(title.Id);
                if (occupant?.Kingdom is null || titleKingdom is null || occupant.Kingdom != titleKingdom) continue;

                bool attainderAlreadyPending = titleKingdom.UnresolvedDecisions
                    .OfType<FeudalAttainderDecision>()
                    .Any(d => d.TitleId == title.Id);
                if (attainderAlreadyPending) continue;

                var attainder = new FeudalAttainderDecision(
                    titleKingdom.RulingClan ?? occupant, title.Id, title.HolderClanId!, title.OccupantClanId!);
                titleKingdom.AddDecision(attainder, true);
            }
        }
    }
}
