using System.Collections.Generic;

namespace DellarteDellaGuerra.Domain.PrivateWars.Model
{
    /// <summary>
    /// A single manufactured clan-vs-clan war pressed under one casus belli. The two
    /// principals are the claimant (attacker) and the title-holder (defender); each fights
    /// with its whole vassal subtree, but the side rosters are NOT stored here — they are
    /// resolved dynamically from the feudal hierarchy (design §18.A).
    ///
    /// MainGoalSettlementId is frozen at declaration (design §18.C). OriginalFiefOwners is the
    /// settlement-to-owner snapshot taken at declaration, used to revert captures on resolution
    /// (status quo ante, design §8). BattleScore is the one *accumulated* score term (capped,
    /// attacker-positive); every other score term is recomputed from current world state each
    /// tick. Score is the last persisted total (for save/load and UI).
    /// </summary>
    public record PrivateWar(
        string Id,
        string AttackerPrincipalClanId,
        string DefenderPrincipalClanId,
        string CasusBelliType,
        string TitleId,
        string MainGoalSettlementId,
        IReadOnlyDictionary<string, string> OriginalFiefOwners,
        float BattleScore,
        float Score,
        float StartDay,
        PrivateWarStatus Status);
}
