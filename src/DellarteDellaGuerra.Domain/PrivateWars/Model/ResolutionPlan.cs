using System.Collections.Generic;

namespace DellarteDellaGuerra.Domain.PrivateWars.Model
{
    /// <summary>
    /// The pure outcome of resolving a private war: which captured fiefs to revert, the prize to
    /// award (attacker victory only), and whether the attacker lost its casus belli. The integration
    /// layer applies these against engine state (skipping eliminated owners, design §8).
    /// </summary>
    public record ResolutionPlan(
        IReadOnlyList<RevertInstruction> Reverts,
        PrizeAward? Prize,
        bool AttackerClaimLost);
}
