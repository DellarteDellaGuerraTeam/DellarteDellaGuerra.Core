namespace DellarteDellaGuerra.Domain.Titles.Model
{
    /**
     * <summary>
     *  A clan both principals could call on, and what the call has to overcome: where the
     *  feudal hierarchy already puts it, and how its leader stands with each of them.
     * </summary>
     * <param name="Allegiance">
     *  The clan's side under the suzerain chain alone. A clan already mustered for one of the
     *  principals has an oath to break rather than a side to pick.
     * </param>
     * <param name="RelationToClaimant">Hero relation to the claimant, -100..100.</param>
     * <param name="RelationToHolder">Hero relation to the title's holder, -100..100.</param>
     */
    public record SupportCandidate(
        string ClanId,
        FeudalAllegiance Allegiance,
        float RelationToClaimant,
        float RelationToHolder);
}
