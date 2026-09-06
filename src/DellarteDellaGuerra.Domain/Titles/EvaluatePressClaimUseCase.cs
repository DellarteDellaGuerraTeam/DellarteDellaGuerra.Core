using System;
using DellarteDellaGuerra.Domain.Titles.Model;

namespace DellarteDellaGuerra.Domain.Titles
{
    /**
     * <summary>
     *  Prices a claim: how badly does the claimant want to press this one, right now. Returns
     *  an appetite score the campaign layer compares against its declaration threshold; a
     *  score of zero means "not while this holds".
     * </summary>
     * <remarks>
     *  A claimant who cannot expect to win does not go to war, so the strength ratio is a
     *  floor rather than a term: below <see cref="MinimumStrengthRatio"/> the claim scores
     *  nothing whatever else is true of it. Above the floor the ratio, the holder's other
     *  commitments and the bad blood between the two houses add to a base set by how good the
     *  claim is.
     *
     *  Both strengths cover the whole side, vassals included — see
     *  <see cref="WarSideStrength"/>. A war declared here drags in each side's vassals
     *  whether or not they were asked, so pricing the two principals alone would misjudge
     *  every matchup between a liege and his own vassal.
     * </remarks>
     */
    public class EvaluatePressClaimUseCase : IEvaluatePressClaimUseCase
    {
        private const float MinimumStrengthRatio = 1.25f;
        private const float RatioWeight = 0.25f;
        private const float DistractionBonus = 0.25f;
        private const float RelationWeight = 0.2f;

        private const float WeakClaimAppetite = 0.35f;
        private const float StrongClaimAppetite = 0.6f;
        private const float DeJureClaimAppetite = 0.8f;

        // How far past the floor the ratio keeps buying appetite: 2.25:1 is already an
        // overwhelming advantage, and beyond it more men do not make the claim any better.
        private const float RatioSurplusCap = 1f;

        public float Execute(ClaimOpportunity opportunity)
        {
            float ratioSurplus = opportunity.DefenderStrength <= 0f
                ? RatioSurplusCap
                : opportunity.AttackerStrength / opportunity.DefenderStrength - MinimumStrengthRatio;
            if (ratioSurplus < 0f) return 0f;

            // Relation is bounded to -100..100, so the worst friendship can do is take 0.2 off
            // the weakest claim's 0.35 — the score cannot go negative.
            return Appetite(opportunity.Strength)
                   + Math.Min(ratioSurplus, RatioSurplusCap) * RatioWeight
                   + (opportunity.DefenderDistracted ? DistractionBonus : 0f)
                   - opportunity.Relation / 100f * RelationWeight;
        }

        private static float Appetite(ClaimStrength strength) => strength switch
        {
            ClaimStrength.DeJure => DeJureClaimAppetite,
            ClaimStrength.Strong => StrongClaimAppetite,
            _ => WeakClaimAppetite
        };
    }
}
