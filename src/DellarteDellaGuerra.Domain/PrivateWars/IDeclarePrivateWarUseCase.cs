using System.Collections.Generic;
using DellarteDellaGuerra.Domain.PrivateWars.Model;

namespace DellarteDellaGuerra.Domain.PrivateWars
{
    public interface IDeclarePrivateWarUseCase
    {
        PrivateWar? Execute(
            string attackerPrincipalClanId,
            string defenderPrincipalClanId,
            ICasusBelli casusBelli,
            string? mainGoalSettlementId,
            IReadOnlyDictionary<string, string> fiefSnapshot,
            float startDay);
    }
}
