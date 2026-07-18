using System.Collections.Generic;

namespace DellarteDellaGuerra.Domain.PrivateWars.Model
{
    public record PrivateWarArmyPlan(
        string GoalSettlementId,
        string LeaderPartyId,
        IReadOnlyList<string> MemberPartyIds);
}
