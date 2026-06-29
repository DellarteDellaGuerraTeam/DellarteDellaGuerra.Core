using System.Linq;
using DellarteDellaGuerra.Domain.Tournament.Jousting.Port;
using DellarteDellaGuerra.Tournament.Jousting.Api.Campaign;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;

namespace DellarteDellaGuerra.Tournament.Api
{
    public class DadgSettlementAccessModel : SettlementAccessModel
    {
        private readonly SettlementAccessModel _inner;
        private readonly IJoustRequirementsProvider _joustRequirementsProvider;

        public DadgSettlementAccessModel(SettlementAccessModel inner,
            IJoustRequirementsProvider joustRequirementsProvider)
        {
            _inner = inner;
            _joustRequirementsProvider = joustRequirementsProvider;
        }

        public override bool CanMainHeroDoSettlementAction(Settlement settlement,
            SettlementAction settlementAction, out bool disableOption, out TextObject disabledText)
        {
            var canDo = _inner.CanMainHeroDoSettlementAction(settlement, settlementAction,
                out disableOption, out disabledText);

            if (settlementAction == SettlementAction.JoinTournament && canDo)
            {
                var tournament = Campaign.Current.TournamentManager.GetTournamentGame(settlement.Town);
                if (tournament is JoustTournament && !tournament.CanBeAParticipant(CharacterObject.PlayerCharacter, true))
                {
                    var requirements = _joustRequirementsProvider.GetRequirements();
                    var skillList = string.Join(", ", requirements.RequiredSkills.Select(r => $"{r.Name} {r.Minimum}+"));
                    disableOption = true;
                    disabledText = new TextObject("{=bX2nT7qW}You lack the jousting skills required to participate ({SKILL_LIST}).");
                    disabledText.SetTextVariable("SKILL_LIST", new TextObject(skillList));
                    return false;
                }
            }

            return canDo;
        }

        public override bool CanMainHeroAccessLocation(Settlement settlement, string locationId,
            out bool disableOption, out TextObject disabledText) =>
            _inner.CanMainHeroAccessLocation(settlement, locationId, out disableOption, out disabledText);

        public override bool IsRequestMeetingOptionAvailable(Settlement settlement,
            out bool disableOption, out TextObject disabledText) =>
            _inner.IsRequestMeetingOptionAvailable(settlement, out disableOption, out disabledText);

        public override void CanMainHeroEnterDungeon(Settlement settlement, out AccessDetails accessDetails) =>
            _inner.CanMainHeroEnterDungeon(settlement, out accessDetails);

        public override void CanMainHeroEnterLordsHall(Settlement settlement, out AccessDetails accessDetails) =>
            _inner.CanMainHeroEnterLordsHall(settlement, out accessDetails);

        public override void CanMainHeroEnterSettlement(Settlement settlement, out AccessDetails accessDetails) =>
            _inner.CanMainHeroEnterSettlement(settlement, out accessDetails);
    }
}
