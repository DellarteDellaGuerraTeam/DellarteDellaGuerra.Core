namespace DellarteDellaGuerra.Domain.Titles.Model
{
    /// <summary>
    /// One entry of a title's ledger: the hero who took the title, or null when it fell vacant,
    /// the campaign day it happened, and how.
    /// </summary>
    public record TitleHolder(string? HeroId, float SinceDay, TitleAcquisition Acquisition);
}
