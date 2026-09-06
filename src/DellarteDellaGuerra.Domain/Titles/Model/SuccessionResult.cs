namespace DellarteDellaGuerra.Domain.Titles.Model
{
    /// <summary>
    /// One title that changed hands on a holder's death. NewHolderHeroId is null when no heir
    /// could be resolved and the title was left vacant.
    /// </summary>
    public record SuccessionResult(
        string TitleId,
        string PreviousHolderHeroId,
        string? NewHolderHeroId);
}
