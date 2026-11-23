using DellarteDellaGuerra.Domain.Titles.Model;

namespace DellarteDellaGuerra.Domain.Titles
{
    /**
     * <summary>
     *  Whether a claim may be pressed against the title's current holder. A strong or de jure
     *  claim always may; a weak one only when the holder is a child or a woman, the holders a
     *  distant claim can hope to unseat.
     * </summary>
     * <remarks>
     *  A child is anyone under 18, vanilla's age of majority. A holder the genealogy cannot
     *  find is treated as neither, so a weak claim against him stays unpressed.
     * </remarks>
     */
    public static class WeakClaimPolicy
    {
        private const float AgeOfMajority = 18f;

        public static bool IsPressable(ClaimStrength strength, HeroNode? holder) =>
            strength != ClaimStrength.Weak
            || (holder is not null && (holder.IsFemale || holder.Age < AgeOfMajority));
    }
}
