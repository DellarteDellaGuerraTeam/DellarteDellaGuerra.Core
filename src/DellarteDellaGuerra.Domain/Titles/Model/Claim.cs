namespace DellarteDellaGuerra.Domain.Titles.Model
{
    public record Claim(string Id, string ClaimantClanId, string TitleId, ClaimStrength Strength, ClaimOrigin Origin);
}
