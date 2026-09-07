using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Titles.Model;

namespace DellarteDellaGuerra.Domain.Titles
{
    /**
     * <summary>
     *  Sends both calls to arms round the realm and reports who answered which. Every candidate
     *  is pulled towards the claimant or the holder by one number; past a threshold it commits,
     *  and short of one it stays where the hierarchy already had it.
     * </summary>
     * <remarks>
     *  Bad blood does most of the work. The gap between what a clan thinks of the claimant and
     *  what it thinks of the holder is worth up to two, against a legitimacy term and a
     *  bandwagon term that together cannot reach one: a house is turned by whom it hates, and
     *  only nudged by whose claim is better and who is winning.
     *
     *  Breaking an oath costs more than picking a side, so a clan the chain already musters
     *  for one of the principals answers only at <see cref="DefectionThreshold"/>, and only
     *  ever to change sides. An uncommitted clan answers at <see cref="RallyThreshold"/>,
     *  which legitimacy and bandwagon can reach between them: a good claim against a failing
     *  house draws men with no grievance to trade on.
     *
     *  Nobody is named for the side he was already on, so the decision holds only the clans
     *  the hierarchy would not have put where they ended up. A war inside one house can
     *  therefore only ever draw men off the holder, its claimant being a cadet branch with no
     *  vassals of its own to lose.
     * </remarks>
     */
    public class SolicitSupportUseCase : ISolicitSupportUseCase
    {
        private const float DefectionThreshold = 1f;
        private const float RallyThreshold = 0.4f;
        private const float BandwagonWeight = 0.5f;
        private const float WeakClaimLegitimacy = 0f;
        private const float StrongClaimLegitimacy = 0.15f;
        private const float DeJureClaimLegitimacy = 0.3f;

        public SupportDecision Execute(ClaimOpportunity opportunity, IReadOnlyCollection<SupportCandidate> candidates)
        {
            var attackerSupporters = new List<string>();
            var defenderSupporters = new List<string>();

            float towardsTheClaimant = Legitimacy(opportunity.Strength) + Bandwagon(opportunity) * BandwagonWeight;

            foreach (var candidate in candidates)
            {
                float pull = (candidate.RelationToClaimant - candidate.RelationToHolder) / 100f + towardsTheClaimant;

                switch (candidate.Allegiance)
                {
                    case FeudalAllegiance.Holder:
                        if (pull >= DefectionThreshold) attackerSupporters.Add(candidate.ClanId);
                        break;
                    case FeudalAllegiance.Claimant:
                        if (pull <= -DefectionThreshold) defenderSupporters.Add(candidate.ClanId);
                        break;
                    default:
                        if (pull >= RallyThreshold) attackerSupporters.Add(candidate.ClanId);
                        else if (pull <= -RallyThreshold) defenderSupporters.Add(candidate.ClanId);
                        break;
                }
            }

            return new SupportDecision(attackerSupporters, defenderSupporters);
        }

        // -1 when the holder's side is the whole fight, 1 when the claimant's is.
        private static float Bandwagon(ClaimOpportunity opportunity)
        {
            float both = opportunity.AttackerStrength + opportunity.DefenderStrength;

            return both <= 0f ? 0f : (opportunity.AttackerStrength - opportunity.DefenderStrength) / both;
        }

        private static float Legitimacy(ClaimStrength strength) => strength switch
        {
            ClaimStrength.DeJure => DeJureClaimLegitimacy,
            ClaimStrength.Strong => StrongClaimLegitimacy,
            _ => WeakClaimLegitimacy
        };
    }
}
