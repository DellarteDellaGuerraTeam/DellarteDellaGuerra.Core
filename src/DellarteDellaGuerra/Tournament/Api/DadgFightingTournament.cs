using System.Linq;
using DellarteDellaGuerra.Domain.Tournament;
using DellarteDellaGuerra.Domain.Tournament.Model;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Core;
using TaleWorlds.SaveSystem;
using Town = TaleWorlds.CampaignSystem.Settlements.Town;

namespace DellarteDellaGuerra.Tournament.Api
{
    public class DadgFightingTournament : FightTournamentGame
    {
        private readonly IGetTournamentRewardUseCase _getTournamentRewardUseCase;
        [SaveableProperty(1000)] private TournamentReward? TournamentReward { get; set; } 

        public DadgFightingTournament(Town town, IGetTournamentRewardUseCase getTournamentRewardUseCase) : base(town)
        {
            _getTournamentRewardUseCase = getTournamentRewardUseCase;
        }

        protected override ItemObject GetTournamentPrize(
            bool includePlayer,
            int lastRecordedLordCountForTournamentPrize)
        {
            // GetTournamentPrize is called in the base contructor before the use case is initialised.
            if (_getTournamentRewardUseCase == null) return Items.All.First();

            if (TournamentReward is not null) return GetRewardItem(TournamentReward);
            
            var participants = GetParticipantCharacters(Town.Settlement, includePlayer)
                .Select(participant => participant.StringId).ToList();

            TournamentReward =
                _getTournamentRewardUseCase.GetTournamentReward(Town.Settlement.StringId, participants);
            return GetRewardItem(TournamentReward);
        }

        private ItemObject GetRewardItem(TournamentReward? reward)
        {
            if (reward is null) return Items.All.First();
            return Items.All.Find(item => item.StringId.Equals(reward.ItemId)) ?? Items.All.First();
        }
        
    }
}