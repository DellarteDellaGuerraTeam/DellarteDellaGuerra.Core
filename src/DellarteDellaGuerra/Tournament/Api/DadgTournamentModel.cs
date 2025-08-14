using DellarteDellaGuerra.Domain.Tournament;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.TournamentGames;

namespace DellarteDellaGuerra.Tournament.Api
{
    public class DadgTournamentModel : DefaultTournamentModel
    {
        private readonly IGetTournamentRewardUseCase _getTournamentRewardUseCase;

        public DadgTournamentModel(IGetTournamentRewardUseCase getTournamentRewardUseCase)
        {
            _getTournamentRewardUseCase = getTournamentRewardUseCase;
        }

        public override TournamentGame CreateTournament(Town town)
        {
            return new DadgFightingTournament(town, _getTournamentRewardUseCase);
        }
    }
}