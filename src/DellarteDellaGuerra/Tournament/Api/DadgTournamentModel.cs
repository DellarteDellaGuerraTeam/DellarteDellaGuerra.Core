using System;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
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
        private readonly ILoggerFactory _loggerFactory;
        private readonly Random _random = new();

        public DadgTournamentModel(IGetTournamentRewardUseCase rewardUseCase, ILoggerFactory loggerFactory)
        {
            _rewardUseCase = rewardUseCase;
            _loggerFactory = loggerFactory;
        }

        public override TournamentGame CreateTournament(Town town)
        {
            // 1 in 3 chance to start a joust tournament, otherwise regular fighting tournament
            if (_random.Next() % 3 == 2) return new JoustTournament(town, _rewardUseCase, _loggerFactory);

            return new DadgFightingTournament(town, _rewardUseCase);
        }
    }
}
