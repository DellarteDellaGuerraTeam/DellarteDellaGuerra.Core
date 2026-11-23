using System.Collections.Generic;

namespace DellarteDellaGuerra.Domain.Titles.Model
{
    /// <summary>
    /// A node of the feudal map: a de jure title with its current holder and its de jure vassals.
    /// OccupantClanId is set only while the seat is contested (occupant differs from holder).
    /// </summary>
    public record FeudalMapEntry(
        string TitleId,
        string Name,
        TitleRank Rank,
        string SeatSettlementId,
        string? HolderClanId,
        IReadOnlyList<FeudalMapEntry> Vassals,
        string? OccupantClanId = null);
}
