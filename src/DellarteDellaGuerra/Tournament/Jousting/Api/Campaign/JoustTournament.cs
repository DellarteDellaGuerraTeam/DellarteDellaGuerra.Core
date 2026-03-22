using DellarteDellaGuerra.Domain.Tournament.Reward;
using DellarteDellaGuerra.Tournament.Api;
using DellarteDellaGuerra.Tournament.Jousting.Api.Missions;
using TaleWorlds.CampaignSystem.Settlements;

namespace DellarteDellaGuerra.Tournament.Jousting.Api.Campaign
{
    public class JoustTournament : DadgFightingTournament
    {
        private const string SceneName = "dadg_joust_v2";

        public JoustTournament(Town town, IGetTournamentRewardUseCase getTournamentRewardUseCase) : base(town,
            getTournamentRewardUseCase)
        {
        }

        public override int MaxTeamSize => 1;

        public override int MaxTeamNumberPerMatch => 2;

        public override void OpenMission(Settlement settlement, bool isPlayerParticipating)
        {
            JoustingMissionManager.OpenJoustingFightMission(SceneName, this, settlement, settlement.Culture,
                isPlayerParticipating);
        }
    }
}