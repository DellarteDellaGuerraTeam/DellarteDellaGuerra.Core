using SandBox.Tournaments;
using SandBox.Tournaments.MissionLogics;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.TournamentGames;

namespace DellarteDellaGuerra.Tournament.Jousting.Api.Missions.MissionLogic
{
    public class JoustTournamentBehaviour : TournamentBehavior
    {
        public JoustTournamentBehaviour(TournamentGame tournamentGame, Settlement settlement,
            ITournamentGameBehavior gameBehavior, bool isPlayerParticipating) : base(tournamentGame, settlement,
            gameBehavior, isPlayerParticipating)
        {
        }
    }
}