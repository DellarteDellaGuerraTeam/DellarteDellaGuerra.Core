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
    IReadOnlyList<FeudalTitleNode> Children,
    int? InitialHeldSinceYear = null,
    IReadOnlyList<FeudalClaimNode>? InitialClaims = null,
    TitleRank? DignityRank = null,
    string? DignityName = null,
    string? AllegianceTitleId = null,
    TitleRank? MapLabelRank = null);

/**
 * <summary>
 * A claim the configuration says a clan already has on a title when the campaign starts.
 * </summary>
 */
internal sealed record FeudalClaimNode(string ClanId, ClaimStrength Strength);
