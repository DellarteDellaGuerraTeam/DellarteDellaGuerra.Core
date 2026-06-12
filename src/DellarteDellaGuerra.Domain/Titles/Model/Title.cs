namespace DellarteDellaGuerra.Domain.Titles.Model
{
    /// <summary>
    /// HolderClanId is the de jure holder of the dignity; OccupantClanId is the de facto
    /// holder of the seat when the two diverge (conquest). ContestedSinceDay is the campaign
    /// day the current occupation began; both are null when the title is uncontested.
    /// </summary>
    public record Title(
        string Id,
        string Name,
        TitleRank Rank,
        string SeatSettlementId,
        string? HolderClanId,
        string? OccupantClanId = null,
        float? ContestedSinceDay = null)
    {
        public Title WithHolder(string? holderClanId) => this with { HolderClanId = holderClanId };

        public Title WithOccupant(string? occupantClanId, float? contestedSinceDay) =>
            this with { OccupantClanId = occupantClanId, ContestedSinceDay = contestedSinceDay };

        public bool IsContested => OccupantClanId is not null && OccupantClanId != HolderClanId;
    }
}