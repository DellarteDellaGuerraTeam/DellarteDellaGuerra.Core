using System;
using DellarteDellaGuerra.Domain.Tournament.Reward;
using DellarteDellaGuerra.Tournament.Jousting.Api.Campaign;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.TournamentGames;

namespace DellarteDellaGuerra.Tournament.Api
{
    public class DadgTournamentModel : DefaultTournamentModel
    {
        private readonly IGetTournamentRewardUseCase _rewardUseCase;
        private readonly Random _random = new();

        public DadgTournamentModel(IGetTournamentRewardUseCase rewardUseCase)
        {
            _rewardUseCase = rewardUseCase;
        }

        public override TournamentGame CreateTournament(Town town)
        {
            // 1 in 3 chance to start a joust tournament, otherwise regular fighting tournament
            if (_random.Next() % 3 == 2) return new JoustTournament(town, _rewardUseCase);

            return new DadgFightingTournament(town, _rewardUseCase);
        }
    }
}