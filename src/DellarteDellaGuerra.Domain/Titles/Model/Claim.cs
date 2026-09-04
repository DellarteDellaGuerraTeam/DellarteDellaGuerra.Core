namespace DellarteDellaGuerra.Domain.Titles.Model
{
    /// <summary>
    /// ClaimantHeroId is the hero the claim belongs to; ClaimantClanId is the clan that hero
    /// belonged to when the claim was recorded, kept denormalised so clan-level consumers can
    /// query without a genealogy lookup. Claims that are inherently clan-level (conquest)
    /// carry a null ClaimantHeroId.
    /// </summary>
    public record Claim(
        string Id,
        string ClaimantClanId,
        string TitleId,
        ClaimStrength Strength,
        ClaimOrigin Origin,
        string? ClaimantHeroId = null);
}
