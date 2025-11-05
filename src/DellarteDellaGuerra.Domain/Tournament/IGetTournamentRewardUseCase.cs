using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Tournament.Model;

namespace DellarteDellaGuerra.Domain.Tournament
{
    public interface IGetTournamentRewardUseCase
    {
        TournamentReward GetTournamentReward(string townId, List<string> participantTroopIds);
    }
}