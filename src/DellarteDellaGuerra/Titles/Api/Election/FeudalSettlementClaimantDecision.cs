using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.Titles.Model;
using DellarteDellaGuerra.Titles.Api;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Settlements;

namespace DellarteDellaGuerra.Titles.Api.Election
{
    // Extends SettlementClaimantDecision so that the chooser is the de jure Duke (or King when
    // there is no intermediate Duke) for the settlement, and support is weighted by feudal
    // distance. De jure claimants are injected as additional candidates.
    public class FeudalSettlementClaimantDecision : SettlementClaimantDecision
    {
        public FeudalSettlementClaimantDecision(
            Clan proposerClan,
            Settlement settlement,
            Hero capturerHero,
            Clan clanToExclude)
            : base(proposerClan, settlement, capturerHero, clanToExclude)
        {
        }

        public override Clan DetermineChooser()
        {
            if (!FeudalServices.IsInitialised) return base.DetermineChooser();

            var structure = FeudalServices.Structure!;
            var titles = FeudalServices.Titles!;
            string? titleId = structure.GetTitleIdBySeat(Settlement.StringId);
            if (titleId is null) return base.DetermineChooser();

            string? current = structure.GetDeJureSuzerainTitleId(titleId);
            while (current is not null)
            {
                var rank = structure.GetRank(current);
                if (rank == TitleRank.Duke || rank == TitleRank.King || rank == TitleRank.Emperor)
                {
                    var title = titles.GetTitle(current);
                    if (title?.HolderClanId is not null)
                    {
                        var holder = Clan.All.FirstOrDefault(c =>
                            c.StringId == title.HolderClanId && !c.IsEliminated);
                        if (holder is not null) return holder;
                    }
                    break;
                }
                current = structure.GetDeJureSuzerainTitleId(current);
            }

            return base.DetermineChooser();
        }

        public override float DetermineSupport(Clan clan, DecisionOutcome candidateOutcome)
        {
            float baseSupport = base.DetermineSupport(clan, candidateOutcome);
            if (!FeudalServices.IsInitialised) return baseSupport;

            float feudalWeight = FeudalServices.ComputeSupport!.Execute(clan.StringId, Settlement.StringId);
            return baseSupport * feudalWeight;
        }

        public override IEnumerable<DecisionOutcome> DetermineInitialCandidates()
        {
            var baseCandidates = base.DetermineInitialCandidates()
                .Cast<SettlementClaimantDecision.ClanAsDecisionOutcome>()
                .ToList();

            if (!FeudalServices.IsInitialised) return baseCandidates;

            var existingClanIds = new System.Collections.Generic.HashSet<string>(
                baseCandidates.Select(c => c.Clan.StringId));
            string? titleId = FeudalServices.Structure!.GetTitleIdBySeat(Settlement.StringId);
            if (titleId is null) return baseCandidates;

            var claimantCandidates = FeudalServices.Claims!.GetClaimsOn(titleId)
                .Select(claim => Clan.All.FirstOrDefault(c =>
                    c.StringId == claim.ClaimantClanId && !c.IsEliminated))
                .Where(clan => clan is not null && !existingClanIds.Contains(clan!.StringId))
                .Select(clan => new SettlementClaimantDecision.ClanAsDecisionOutcome(clan!));

            return baseCandidates.Concat(claimantCandidates);
        }
    }
}
