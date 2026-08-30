using System.Linq;
using DellarteDellaGuerra.Domain.Tournament.Reward;
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
        [SaveableField(1000)] private SaveableTournamentReward? _savedReward;

        public DadgFightingTournament(Town town, IGetTournamentRewardUseCase getTournamentRewardUseCase) : base(town)
        {
            _getTournamentRewardUseCase = getTournamentRewardUseCase;
        }

        protected override ItemObject GetTournamentPrize(
            bool includePlayer,
            int lastRecordedLordCountForTournamentPrize)
        {
            // A reloaded tournament is deserialised without its constructor, so the use case is
            // null; the persisted reward is the source of truth in that case.
            if (_savedReward is not null) return GetRewardItem(_savedReward);

            // GetTournamentPrize is called in the base constructor before the use case is initialised.
            if (_getTournamentRewardUseCase == null) return Items.All.First();

            var participants = GetParticipantCharacters(Town.Settlement, includePlayer)
                .Select(participant => participant.StringId).ToList();

            var reward = _getTournamentRewardUseCase.GetTournamentReward(Town.Settlement.StringId, participants);
            _savedReward = reward is not null ? new SaveableTournamentReward { ItemId = reward.ItemId } : null;
            return GetRewardItem(_savedReward);
        }

        private ItemObject GetRewardItem(SaveableTournamentReward? reward)
        {
            if (reward?.ItemId is null) return Items.All.First();
            return Items.All.Find(item => item.StringId.Equals(reward.ItemId)) ?? Items.All.First();
        }
        
    }
}