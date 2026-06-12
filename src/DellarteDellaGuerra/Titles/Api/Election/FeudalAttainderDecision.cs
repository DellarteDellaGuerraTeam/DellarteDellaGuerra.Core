using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Titles.Model;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace DellarteDellaGuerra.Titles.Api.Election
{
    // A KingdomDecision where the king rules on a contested title: an act of attainder strips
    // the dispossessed de jure holder and regrants the dignity to the clan occupying its seat;
    // upholding the dignity leaves the holder in place (and restarts the contested clock).
    public class FeudalAttainderDecision : KingdomDecision
    {
        private sealed class EmptyImageIdentifier : ImageIdentifier { }

        public sealed class AttaintOutcome : DecisionOutcome
        {
            public override TextObject GetDecisionTitle() => new TextObject("Attaint and Regrant");
            public override TextObject GetDecisionDescription() => new TextObject("Strip the dispossessed holder of the dignity and grant it to the occupant of the seat.");
            public override string GetDecisionLink() => string.Empty;
            public override ImageIdentifier GetDecisionImageIdentifier() => new EmptyImageIdentifier();
        }

        public sealed class UpholdOutcome : DecisionOutcome
        {
            public override TextObject GetDecisionTitle() => new TextObject("Uphold the Dignity");
            public override TextObject GetDecisionDescription() => new TextObject("The dispossessed holder keeps the title; occupation of the seat grants no right.");
            public override string GetDecisionLink() => string.Empty;
            public override ImageIdentifier GetDecisionImageIdentifier() => new EmptyImageIdentifier();
        }

        [SaveableField(1)]
        private string _titleId = string.Empty;

        [SaveableField(2)]
        private string _holderClanId = string.Empty;

        [SaveableField(3)]
        private string _occupantClanId = string.Empty;

        public string TitleId => _titleId;

        public FeudalAttainderDecision(Clan proposerClan, string titleId, string holderClanId, string occupantClanId)
            : base(proposerClan)
        {
            _titleId = titleId;
            _holderClanId = holderClanId;
            _occupantClanId = occupantClanId;
            NotifyPlayer = holderClanId == Clan.PlayerClan.StringId
                           || occupantClanId == Clan.PlayerClan.StringId
                           || Kingdom?.RulingClan == Clan.PlayerClan;
        }

        // An act of attainder is parliament assembling, not a council vote: leave the
        // dispossessed holder a window to retake the seat and moot the act.
        protected override int HoursToWait => 168;

        public override bool IsAllowed() => FeudalServices.IsInitialised && Kingdom is not null;

        protected override bool ShouldBeCancelledInternal()
        {
            var title = FeudalServices.Titles?.GetTitle(_titleId);
            if (title is null || !title.IsContested) return true;
            if (title.HolderClanId != _holderClanId || title.OccupantClanId != _occupantClanId) return true;

            // A title is a dignity of the kingdom: a foreign occupant cannot be regularised.
            var occupant = FeudalTitleKingdoms.FindClan(_occupantClanId);
            return occupant is null || occupant.Kingdom != Kingdom;
        }

        public override int GetProposalInfluenceCost() => 0;

        public override TextObject GetGeneralTitle() => new TextObject("Act of Attainder");

        public override TextObject GetSupportTitle() => new TextObject("Support the Attainder");

        public override TextObject GetChooseTitle() => new TextObject("Rule on the Attainder");

        public override TextObject GetSupportDescription()
        {
            return new TextObject($"{OccupantName()} petitions for {TitleName()}, held by the dispossessed {HolderName()}.");
        }

        public override TextObject GetChooseDescription()
        {
            return new TextObject($"Rule whether {HolderName()} is attainted and {TitleName()} regranted to {OccupantName()}.");
        }

        public override Clan DetermineChooser() => Kingdom?.RulingClan ?? ProposerClan;

        public override IEnumerable<DecisionOutcome> DetermineInitialCandidates()
        {
            return new DecisionOutcome[] { new AttaintOutcome(), new UpholdOutcome() };
        }

        public override float DetermineSupport(Clan clan, DecisionOutcome possibleOutcome)
        {
            bool isOccupant = clan.StringId == _occupantClanId;
            bool isHolder = clan.StringId == _holderClanId;

            if (possibleOutcome is AttaintOutcome)
            {
                if (isOccupant) return 1.0f;
                if (isHolder) return -1.0f;

                // Lords back whoever actually exercises lordship near them.
                var title = FeudalServices.Titles?.GetTitle(_titleId);
                if (title is not null && FeudalServices.ComputeSupport is not null)
                {
                    return 0.3f * FeudalServices.ComputeSupport.Execute(clan.StringId, title.SeatSettlementId);
                }

                return 0.2f;
            }

            if (isHolder) return 1.0f;
            if (isOccupant) return -1.0f;
            return 0.3f;
        }

        public override void DetermineSponsors(MBReadOnlyList<DecisionOutcome> possibleOutcomes)
        {
            var occupant = FeudalTitleKingdoms.FindClan(_occupantClanId);
            var holder = FeudalTitleKingdoms.FindClan(_holderClanId);

            foreach (var outcome in possibleOutcomes)
            {
                if (outcome is AttaintOutcome && occupant is not null && occupant.Kingdom == Kingdom)
                    outcome.SetSponsor(occupant);
                else if (outcome is UpholdOutcome && holder is not null && holder.Kingdom == Kingdom)
                    outcome.SetSponsor(holder);
                else
                    AssignDefaultSponsor(outcome);
            }
        }

        public override void ApplyChosenOutcome(DecisionOutcome chosenOutcome)
        {
            if (!FeudalServices.IsInitialised) return;

            var title = FeudalServices.Titles!.GetTitle(_titleId);
            if (title is null || !title.IsContested) return;

            float today = (float)CampaignTime.Now.ToDays;
            if (chosenOutcome is AttaintOutcome)
            {
                FeudalServices.AssignTitle?.Execute(
                    title.SeatSettlementId, _occupantClanId, SeatTransferKind.Grant, today);
            }
            else
            {
                // The act re-legitimises the status quo: restart the contested clock so the
                // question is not re-put to parliament the next day.
                FeudalServices.Titles.SaveTitle(title.WithOccupant(title.OccupantClanId, today));
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
            return chosenOutcome is AttaintOutcome
                ? new TextObject($"{HolderName()} was attainted; {TitleName()} passes to {OccupantName()}.")
                : new TextObject($"The dignity of {TitleName()} was upheld for {HolderName()}.");
        }

        public override DecisionOutcome GetQueriedDecisionOutcome(MBReadOnlyList<DecisionOutcome> possibleOutcomes)
            => possibleOutcomes[0];

        private string TitleName() => FeudalServices.Structure?.GetTitleName(_titleId) ?? _titleId;

        private string HolderName() => FeudalTitleKingdoms.FindClan(_holderClanId)?.Name?.ToString() ?? _holderClanId;

        private string OccupantName() => FeudalTitleKingdoms.FindClan(_occupantClanId)?.Name?.ToString() ?? _occupantClanId;
    }
}