using System;
using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Titles.Model;

namespace DellarteDellaGuerra.Domain.Titles
{
    /**
     * <summary>
     *  Picks the houses both calls to arms go out to, and where the feudal hierarchy already
     *  puts each of them.
     * </summary>
     * <remarks>
     *  A house the suzerain chain musters for either principal is asked wherever it sits,
     *  because <see cref="WarSideStrength"/> counts it wherever it sits: a vassal that pricing
     *  puts on a side must also get the chance to defect from it. A house the chain leaves out
     *  is asked only if it belongs to the realm the war is fought in, or if its leader has a
     *  personal bond with one of the principals (see <see cref="PersonalBondPolicy"/>): a foreign ally
     *  may ride to a friend's quarrel, but an uncommitted stranger cannot rally to one that is
     *  none of his.
     * </remarks>
     */
    public static class SupportCandidacy
    {
        private static readonly IReadOnlyCollection<string> NoSupporters = Array.Empty<string>();

        public static IEnumerable<(string ClanId, FeudalAllegiance Allegiance)> Select(
            IEnumerable<string> clanIds,
            Func<string, bool> mayRally,
            Func<string, string?> getSuzerain,
            string attackerSideId,
            string defenderClanId)
        {
            foreach (string clanId in clanIds)
            {
                string? side = WarSideStrength.ResolveSide(
                    clanId, getSuzerain, attackerSideId, defenderClanId, NoSupporters, NoSupporters);

                if (side == attackerSideId) yield return (clanId, FeudalAllegiance.Claimant);
                else if (side == defenderClanId) yield return (clanId, FeudalAllegiance.Holder);
                else if (mayRally(clanId)) yield return (clanId, FeudalAllegiance.Uncommitted);
            }
        }
    }
}
