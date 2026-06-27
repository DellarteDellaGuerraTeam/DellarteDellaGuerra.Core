using System.Linq;
using DellarteDellaGuerra.Domain.Tournament.Jousting.Port;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Localization;
using BannerlordCampaign = TaleWorlds.CampaignSystem.Campaign;

namespace DellarteDellaGuerra.Tournament.Jousting.Api.Campaign
{
    public class JoustTournamentCampaignBehavior : CampaignBehaviorBase
    {
        private readonly IJoustRequirementsProvider _joustRequirementsProvider;

        public IJoustRequirementsProvider JoustRequirementsProvider => _joustRequirementsProvider;

        public JoustTournamentCampaignBehavior(IJoustRequirementsProvider joustRequirementsProvider)
        {
            _joustRequirementsProvider = joustRequirementsProvider;
        }

        public override void RegisterEvents() =>
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);

        public override void SyncData(IDataStore dataStore) { }

        private void OnSessionLaunched(CampaignGameStarter starter) => AddDialogs(starter);

        private void AddDialogs(CampaignGameStarter starter)
        {
            var requirements = _joustRequirementsProvider.GetRequirements();
            var skillList = string.Join(", ", requirements.RequiredSkills.Select(r => $"{r.Name} {r.Minimum}"));
            var dialogText = new TextObject("{=jR4mK9pL}My apologies, friend, but I cannot enter your name. This is a jousting tournament, only seasoned riders and lancers may take to the field. ({SKILL_LIST})");
            dialogText.SetTextVariable("SKILL_LIST", new TextObject(skillList));
            starter.AddDialogLine(
                "dadg_joust_entry_decline",
                "arena_master_enter_tournament",
                "arena_master_talk",
                dialogText.ToString(),
                IsJoustTournamentAndPlayerLacksSkills,
                null,
                200);
        }

        private static bool IsJoustTournamentAndPlayerLacksSkills()
        {
            var town = Settlement.CurrentSettlement?.Town;
            if (town == null) return false;

            var tournament = BannerlordCampaign.Current.TournamentManager.GetTournamentGame(town);
            return tournament != null && !tournament.CanBeAParticipant(CharacterObject.PlayerCharacter, true);
        }
    }
}
