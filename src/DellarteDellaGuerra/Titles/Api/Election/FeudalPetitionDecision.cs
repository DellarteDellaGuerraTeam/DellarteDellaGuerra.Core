using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.Titles.Model;
using DellarteDellaGuerra.Titles.Api;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace DellarteDellaGuerra.Titles.Api.Election
{
    // A KingdomDecision where a claimant petitions their suzerain to recognise their claim.
    // Granting upgrades the claim; denying causes a tension surge that may later trigger open war.
    public class FeudalPetitionDecision : KingdomDecision
    {
        private sealed class EmptyImageIdentifier : ImageIdentifier { }

        public sealed class GrantClaimOutcome : DecisionOutcome
        {
            public override TextObject GetDecisionTitle() => new TextObject("Grant Petition");
            public override TextObject GetDecisionDescription() => new TextObject("Recognise the claimant's right to hold the title.");
            public override string GetDecisionLink() => string.Empty;
            public override ImageIdentifier GetDecisionImageIdentifier() => new EmptyImageIdentifier();
        }

        public sealed class DenyClaimOutcome : DecisionOutcome
        {
            public override TextObject GetDecisionTitle() => new TextObject("Deny Petition");
            public override TextObject GetDecisionDescription() => new TextObject("Refuse to recognise the claimant's petition.");
            public override string GetDecisionLink() => string.Empty;
            public override ImageIdentifier GetDecisionImageIdentifier() => new EmptyImageIdentifier();
        }

        [SaveableField(1)]
        private string _titleId = string.Empty;

        [SaveableField(2)]
        private string _suzerainClanId = string.Empty;

        public FeudalPetitionDecision(Clan claimantClan, Clan suzerainClan, string titleId)
            : base(claimantClan)
        {
            _titleId = titleId;
            _suzerainClanId = suzerainClan.StringId;
            NotifyPlayer = claimantClan == Clan.PlayerClan || suzerainClan == Clan.PlayerClan;
        }

        public override bool IsAllowed() => FeudalServices.IsInitialised && Kingdom is not null;

        public override int GetProposalInfluenceCost() => 0;

        public override TextObject GetGeneralTitle() => new TextObject("Feudal Petition");

        public override TextObject GetSupportTitle() => new TextObject("Support Petition");

        public override TextObject GetChooseTitle() => new TextObject("Rule on Petition");

        public override TextObject GetSupportDescription()
        {
            string titleName = FeudalServices.Structure?.GetTitleName(_titleId) ?? _titleId;
            return new TextObject($"{ProposerClan?.Name} petitions for the title of {titleName}.");
        }

        public override TextObject GetChooseDescription()
        {
            string titleName = FeudalServices.Structure?.GetTitleName(_titleId) ?? _titleId;
            return new TextObject($"Rule on the petition of {ProposerClan?.Name} for {titleName}.");
        }

        public override Clan DetermineChooser()
        {
            // Fall back to the kingdom's ruling clan if the suzerain is eliminated
            var suzerain = Clan.All.FirstOrDefault(c => c.StringId == _suzerainClanId && !c.IsEliminated);
            return suzerain ?? Kingdom?.RulingClan ?? ProposerClan;
        }

        public override IEnumerable<DecisionOutcome> DetermineInitialCandidates()
        {
            return new DecisionOutcome[] { new GrantClaimOutcome(), new DenyClaimOutcome() };
        }

        public override float DetermineSupport(Clan clan, DecisionOutcome possibleOutcome)
        {
            if (possibleOutcome is GrantClaimOutcome)
            {
                if (clan.StringId == _suzerainClanId)
                {
                    var claim = FeudalServices.Claims?
                        .GetClaimsFor(ProposerClan.StringId)
                        .FirstOrDefault(c => c.TitleId == _titleId);
                    return claim?.Strength switch
                    {
                        ClaimStrength.DeJure => 0.6f,
                        ClaimStrength.Strong => 0.35f,
                        _ => 0.15f
                    };
                }
                return 0.1f;
            }

            return clan.StringId == _suzerainClanId ? 0.15f : 0.5f;
        }

        public override void DetermineSponsors(MBReadOnlyList<DecisionOutcome> possibleOutcomes)
        {
            foreach (var outcome in possibleOutcomes)
            {
                if (outcome is GrantClaimOutcome)
                    outcome.SetSponsor(ProposerClan);
                else
                    AssignDefaultSponsor(outcome);
            }
        }

        public override void ApplyChosenOutcome(DecisionOutcome chosenOutcome)
        {
            if (!FeudalServices.IsInitialised) return;

            if (chosenOutcome is GrantClaimOutcome)
            {
                UpgradeOrAddClaim();
                FeudalServices.Tensions?.ResetTension(ProposerClan.StringId, _titleId);
            }
            else if (chosenOutcome is DenyClaimOutcome)
            {
                SurgeTensionOnDenial();
            }
        }

        public override TextObject GetSecondaryEffects() => new TextObject(string.Empty);

        public override void ApplySecondaryEffects(
            MBReadOnlyList<DecisionOutcome> possibleOutcomes,
            DecisionOutcome chosenOutcome) { }

        public override TextObject GetChosenOutcomeText(
            DecisionOutcome chosenOutcome,
            SupportStatus supportStatus,
            bool isShortVersion)
        {
            string titleName = FeudalServices.Structure?.GetTitleName(_titleId) ?? _titleId;
            return chosenOutcome is GrantClaimOutcome
                ? new TextObject($"The petition of {ProposerClan?.Name} for {titleName} was granted.")
                : new TextObject($"The petition of {ProposerClan?.Name} for {titleName} was denied.");
        }

        public override DecisionOutcome GetQueriedDecisionOutcome(MBReadOnlyList<DecisionOutcome> possibleOutcomes)
            => possibleOutcomes[0];

        private void UpgradeOrAddClaim()
        {
            var existing = FeudalServices.Claims!.GetClaimsFor(ProposerClan.StringId)
                .FirstOrDefault(c => c.TitleId == _titleId);
            if (existing is not null)
            {
                FeudalServices.Claims.RemoveClaim(existing.Id);
                var upgraded = existing.Strength < ClaimStrength.Strong
                    ? existing with { Strength = ClaimStrength.Strong }
                    : existing;
                FeudalServices.Claims.AddClaim(upgraded);
            }
            else
            {
                var newClaim = new Claim(
                    $"claim_{ProposerClan.StringId}_{_titleId}",
                    ProposerClan.StringId,
                    _titleId,
                    ClaimStrength.Weak,
                    ClaimOrigin.Conquest);
                FeudalServices.Claims!.AddClaim(newClaim);
            }
        }

        private void SurgeTensionOnDenial()
        {
            var claim = FeudalServices.Claims?.GetClaimsFor(ProposerClan.StringId)
                .FirstOrDefault(c => c.TitleId == _titleId);
            if (claim is not null)
                FeudalServices.AccumulateTension?.Execute(ProposerClan.StringId, claim, 5.0f);
        }
    }
}
