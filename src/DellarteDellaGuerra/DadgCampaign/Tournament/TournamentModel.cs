using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.TournamentGames;

namespace DellarteDellaGuerra.DadgCampaign.Tournament
{
    public class TournamentModel : DefaultTournamentModel
    {
        public override TournamentGame CreateTournament(Town town)
        {
            return new TourneyTournament(town);
        }
    }
}