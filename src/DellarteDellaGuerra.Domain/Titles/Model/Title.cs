namespace DellarteDellaGuerra.Domain.Titles.Model
{
    /// <summary>
    /// HolderHeroId is the de jure holder of the dignity, a hero whose clan is derived through
    /// IGenealogy.GetClanOf; OccupantClanId is the de facto holder of the seat when the two
    /// diverge (conquest) — it stays a clan because de facto occupation derives from
    /// Settlement.OwnerClan, which has no hero equivalent. ContestedSinceDay is the campaign
    /// day the current occupation began; both are null when the title is uncontested.
    /// </summary>
    public record Title(
        string Id,
        string Name,
        TitleRank Rank,
        string SeatSettlementId,
        string? HolderHeroId,
        string? OccupantClanId = null,
        float? ContestedSinceDay = null)
    {
        public Title WithHolder(string? holderHeroId) => this with { HolderHeroId = holderHeroId };

        public Title WithOccupant(string? occupantClanId, float? contestedSinceDay) =>
            this with { OccupantClanId = occupantClanId, ContestedSinceDay = contestedSinceDay };

        public bool IsContested => OccupantClanId is not null;
    }
}