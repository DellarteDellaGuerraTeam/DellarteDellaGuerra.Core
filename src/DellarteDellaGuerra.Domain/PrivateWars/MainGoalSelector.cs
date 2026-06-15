using System;
using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.PrivateWars.Model;

namespace DellarteDellaGuerra.Domain.PrivateWars
{
    /// <summary>
    /// Picks the war's main goal — the settlement whose control drives fatigue — and freezes it at
    /// declaration (design §18.C): the highest-prosperity de jure settlement of the claimed title
    /// that the defendant currently holds; if it holds none, the defendant's capital. Returns null
    /// when the defendant is landless (no capital), which forbids pressing the casus belli (§18.D).
    /// </summary>
    public class MainGoalSelector
    {
        public string? Select(
            IEnumerable<SettlementInfo> titleDeJureSettlements,
            string defenderClanId,
            string? defenderCapitalId)
        {
            var heldByDefender = titleDeJureSettlements
                .Where(s => s.OwnerClanId == defenderClanId)
                .OrderByDescending(s => s.Prosperity)
                .ThenBy(s => s.Id, StringComparer.Ordinal)
                .ToList();

            return heldByDefender.Count > 0 ? heldByDefender[0].Id : defenderCapitalId;
        }
    }
}
