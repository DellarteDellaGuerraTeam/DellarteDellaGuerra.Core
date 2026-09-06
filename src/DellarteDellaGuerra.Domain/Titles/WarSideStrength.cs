using System;
using System.Collections.Generic;

namespace DellarteDellaGuerra.Domain.Titles
{
    /**
     * <summary>
     *  Sums the military strength each side of a prospective war would actually field, by
     *  assigning every clan in the realm to the first belligerent it meets walking up its
     *  suzerain chain.
     * </summary>
     * <remarks>
     *  This is the rule the private-war registry resolves sides by once a war exists, applied
     *  ahead of the declaration so that the claimant can price the fight he is about to
     *  start. Nearest ancestor wins, so a vassal of the attacker who is himself a vassal of
     *  the defender further up fights for the attacker. A clan under neither counts for
     *  neither.
     * </remarks>
     */
    public static class WarSideStrength
    {
        public static (float Attacker, float Defender) Sum(
            IReadOnlyDictionary<string, float> strengthByClanId,
            Func<string, string?> getSuzerain,
            string attackerClanId,
            string defenderClanId)
        {
            float attacker = 0f;
            float defender = 0f;

            foreach (var entry in strengthByClanId)
            {
                string? side = ResolveSide(entry.Key, getSuzerain, attackerClanId, defenderClanId);
                if (side == attackerClanId) attacker += entry.Value;
                else if (side == defenderClanId) defender += entry.Value;
            }

            return (attacker, defender);
        }

        private static string? ResolveSide(
            string clanId,
            Func<string, string?> getSuzerain,
            string attackerClanId,
            string defenderClanId)
        {
            var visited = new HashSet<string>();
            string? current = clanId;

            // The chain is authored data, so it can loop; Add() failing ends the walk.
            while (current != null && visited.Add(current))
            {
                if (current == attackerClanId) return attackerClanId;
                if (current == defenderClanId) return defenderClanId;
                current = getSuzerain(current);
            }

            return null;
        }
    }
}
