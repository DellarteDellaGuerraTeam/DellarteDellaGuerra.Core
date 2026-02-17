using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Tournament.Reward.Model;

namespace DellarteDellaGuerra.Domain.Tournament.Reward
{
    public interface IGetTournamentRewardUseCase
    {
        TournamentReward GetTournamentReward(string townId, List<string> participantTroopIds);
    }
}