namespace DellarteDellaGuerra.Domain.Titles.Model
{
    /**
     * <summary>
     *  A claimant house's best claim on a held title, flattened into the facts that decide
     *  whether it can become a war at all. Built by the campaign layer from the live game state
     *  so that ScreenClaimUseCase stays pure.
     * </summary>
     * <param name="Holder">The title's holder, or null when the genealogy cannot find him.</param>
     * <param name="RulingClanId">The ruling house of the claimant's realm, if it has one.</param>
     * <param name="HasPretender">Whether a hero is behind a claim on the house's own dignity.</param>
     * <param name="ClaimantAtPrivateWar">Whether the claimant house is already fighting a private war.</param>
     */
    public record ClaimCandidacy(
        string ClaimantClanId,
        string DefenderClanId,
        ClaimStrength Strength,
        HeroNode? Holder,
        string? PlayerClanId,
        string? RulingClanId,
        string? ClaimantKingdomId,
        string? DefenderKingdomId,
        bool HasPretender,
        bool ClaimantAtPrivateWar);
}
