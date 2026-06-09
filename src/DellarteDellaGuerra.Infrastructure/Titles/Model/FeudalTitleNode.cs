using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Titles.Model;

namespace DellarteDellaGuerra.Infrastructure.Titles.Model;

/**
 * <summary>
 * A node of the de jure feudal hierarchy tree parsed from the titles configuration file.
 * </summary>
 */
internal sealed record FeudalTitleNode(
    string TitleId,
    string Name,
    TitleRank Rank,
    string SeatSettlementId,
    string? InitialHolderClanId,
    IReadOnlyList<FeudalTitleNode> Children);
