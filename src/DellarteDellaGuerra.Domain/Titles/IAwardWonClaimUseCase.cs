using System.Collections.Generic;

namespace DellarteDellaGuerra.Domain.Titles
{
    public interface IAwardWonClaimUseCase
    {
        IReadOnlyList<string> Execute(string titleId, string winnerClanId, string loserClanId);
    }
}
