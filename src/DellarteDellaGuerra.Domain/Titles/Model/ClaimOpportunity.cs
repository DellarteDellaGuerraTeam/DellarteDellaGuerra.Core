namespace DellarteDellaGuerra.Domain.Titles.Model
{
    /**
     * <summary>
     *  A claim the campaign has already found worth pricing, flattened into the facts the
     *  appetite score is computed from. Built by the campaign layer from the live game state
     *  so that EvaluatePressClaimUseCase stays pure.
     * </summary>
     * <param name="Strength">Strength of the claimant clan's best claim on the title.</param>
     * <param name="AttackerStrength">Military strength of the claimant's side, vassals included.</param>
     * <param name="DefenderStrength">Military strength of the holder's side, vassals included.</param>
     * <param name="DefenderDistracted">Whether the holder is already committed elsewhere.</param>
     * <param name="Relation">Hero relation between the two clan leaders, -100..100.</param>
     */
    public record ClaimOpportunity(
        string TitleId,
        string AttackerClanId,
        string DefenderClanId,
        ClaimStrength Strength,
        float AttackerStrength,
        float DefenderStrength,
        bool DefenderDistracted,
        float Relation);
}
