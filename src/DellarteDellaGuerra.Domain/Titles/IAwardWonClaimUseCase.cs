using DellarteDellaGuerra.Domain.Titles.Model;

namespace DellarteDellaGuerra.Domain.Titles
{
    public interface IAwardWonClaimUseCase
    {
        WonClaimAward Execute(string titleId, string winnerClanId, string loserClanId, float currentDay);
    }
}
