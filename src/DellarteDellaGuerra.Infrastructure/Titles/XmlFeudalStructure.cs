using System.Collections.Generic;
using System.Threading;
using DellarteDellaGuerra.Domain.Titles.Model;
using DellarteDellaGuerra.Domain.Titles.Port;
using DellarteDellaGuerra.Infrastructure.Titles.Model;

namespace DellarteDellaGuerra.Infrastructure.Titles;

/**
 * <summary>
 * The de jure feudal hierarchy backed by the forest parsed from the titles configuration XML,
 * with the titles since reattached to another realm moved to their new suzerain.
 * </summary>
 */
public class XmlFeudalStructure : IFeudalStructure
{
    private long _revision;
    public long Revision => Interlocked.Read(ref _revision);
    private static readonly IReadOnlyList<string> NoVassals = new List<string>();
    private static readonly IReadOnlyList<FeudalClaimNode> NoClaims = new List<FeudalClaimNode>();

    // The default campaign calendar (4 seasons of 21 days), on which campaign days are counted
    // from year 0, so a configured year lands on the same scale as CampaignTime.Now.ToDays.
    private const int DaysInYear = 84;

    private readonly Dictionary<string, FeudalTitleNode> _nodesByTitleId = new Dictionary<string, FeudalTitleNode>();
    private readonly Dictionary<string, string> _parentTitleIdByTitleId = new Dictionary<string, string>();
    private readonly Dictionary<string, string> _titleIdBySeatSettlementId = new Dictionary<string, string>();
    private readonly Dictionary<string, List<string>> _childTitleIdsByTitleId = new Dictionary<string, List<string>>();
    private readonly List<string> _allTitleIds = new List<string>();
    private readonly IReadOnlyList<FeudalTitleNode> _forest;
    private readonly Dictionary<string, string?> _reattachedSuzerainByTitleId = new Dictionary<string, string?>();

    internal XmlFeudalStructure(IReadOnlyList<FeudalTitleNode> forest)
    {
        _forest = forest;
        IndexForest();
    }

    public string? GetDeJureSuzerainTitleId(string titleId)
    {
        return _parentTitleIdByTitleId.TryGetValue(titleId, out string parentTitleId) ? parentTitleId : null;
    }

    public string? GetAllegianceSuzerainTitleId(string titleId)
    {
        // A conquest's saved reattachment takes precedence over the initial allegiance.
        if (_reattachedSuzerainByTitleId.TryGetValue(titleId, out string? reattached)) return reattached;
        if (_nodesByTitleId.TryGetValue(titleId, out FeudalTitleNode node)
            && node.AllegianceTitleId is { } allegiance
            && RealmOf(titleId) == RealmOf(allegiance)) return allegiance;
        return GetDeJureSuzerainTitleId(titleId);
    }

    private string RealmOf(string titleId)
    {
        var seen = new HashSet<string> { titleId };
        while (GetDeJureSuzerainTitleId(titleId) is { } parent && seen.Add(parent)) titleId = parent;
        return titleId;
    }

    public IReadOnlyList<string> GetDeJureVassalTitleIds(string titleId)
    {
        return _childTitleIdsByTitleId.TryGetValue(titleId, out List<string> childTitleIds)
            ? childTitleIds
            : NoVassals;
    }

    public string? GetTitleIdBySeat(string settlementId)
    {
        return _titleIdBySeatSettlementId.TryGetValue(settlementId, out string titleId) ? titleId : null;
    }

    public IReadOnlyList<string> GetAllTitleIds()
    {
        return _allTitleIds;
    }

    public TitleRank? GetRank(string titleId)
    {
        return _nodesByTitleId.TryGetValue(titleId, out FeudalTitleNode node) ? node.Rank : (TitleRank?) null;
    }

    public string? GetTitleName(string titleId)
    {
        return _nodesByTitleId.TryGetValue(titleId, out FeudalTitleNode node) ? node.Name : null;
    }

    public TitleRank? GetMapLabelRank(string titleId) =>
        _nodesByTitleId.TryGetValue(titleId, out var node) ? node.MapLabelRank ?? node.Rank : null;

    public void Reattach(string titleId, string? suzerainTitleId) => TryReattach(titleId, suzerainTitleId);

    private bool TryReattach(string titleId, string? suzerainTitleId)
    {
        if (!_nodesByTitleId.ContainsKey(titleId) || (suzerainTitleId is not null && !_nodesByTitleId.ContainsKey(suzerainTitleId)))
            return false;
        var seen = new HashSet<string> { titleId };
        for (string? parent = suzerainTitleId; parent is not null; parent = GetDeJureSuzerainTitleId(parent))
            if (!seen.Add(parent)) return false;

        if (GetDeJureSuzerainTitleId(titleId) is { } formerSuzerainTitleId)
        {
            _childTitleIdsByTitleId[formerSuzerainTitleId].Remove(titleId);
        }

        _parentTitleIdByTitleId.Remove(titleId);
        if (suzerainTitleId is not null)
        {
            _parentTitleIdByTitleId[titleId] = suzerainTitleId;
            _childTitleIdsByTitleId[suzerainTitleId].Add(titleId);
        }

        _reattachedSuzerainByTitleId[titleId] = suzerainTitleId;
        Interlocked.Increment(ref _revision);
        return true;
    }

    /**
     * <summary>
     * Returns the titles reattached since the configured hierarchy, each with its new suzerain
     * (null for a root), typically for save game serialisation.
     * </summary>
     */
    public IReadOnlyDictionary<string, string?> SnapshotReattachments()
    {
        return new Dictionary<string, string?>(_reattachedSuzerainByTitleId);
    }

    /**
     * <summary>
     * Resets the hierarchy to the configured one and reapplies the given reattachments,
     * typically on campaign start or save load.
     * </summary>
     */
    public void InitialiseReattachments(IReadOnlyDictionary<string, string?> reattachments)
    {
        _nodesByTitleId.Clear();
        _parentTitleIdByTitleId.Clear();
        _titleIdBySeatSettlementId.Clear();
        _childTitleIdsByTitleId.Clear();
        _allTitleIds.Clear();
        _reattachedSuzerainByTitleId.Clear();
        IndexForest();

        // A valid saved move can depend on another title moving out of its old subtree first.
        var pending = new List<KeyValuePair<string, string?>>(reattachments);
        while (pending.Count > 0)
        {
            if (pending.RemoveAll(move => TryReattach(move.Key, move.Value)) == 0) break;
        }
        Interlocked.Increment(ref _revision);
    }

    /**
     * <summary>
     * Builds the initial titles from the configured hierarchy,
     * each held by the configured holder hero when he belongs to the initial holder clan,
     * otherwise by that clan's leader, if any,
     * since the configured year (day 0 when none is given).
     * </summary>
     */
    public IReadOnlyList<Title> BuildInitialTitles(IGenealogy genealogy)
    {
        var titles = new List<Title>(_allTitleIds.Count);
        foreach (string titleId in _allTitleIds)
        {
            FeudalTitleNode node = _nodesByTitleId[titleId];
            string? holderHeroId = node.InitialHolderClanId is not { } clanId ? null
                : node.InitialHolderHeroId is { } heroId && genealogy.GetClanOf(heroId) == clanId ? heroId
                : genealogy.GetClanLeaderId(clanId);
            float heldSinceDay = (node.InitialHeldSinceYear ?? 0) * DaysInYear;
            titles.Add(new Title(
                node.TitleId, node.DignityName ?? node.Name, node.DignityRank ?? node.Rank, node.SeatSettlementId, holderHeroId,
                heldSinceDay: heldSinceDay));
        }

        return titles;
    }

    /**
     * <summary>
     * Builds the claims the configuration gives clans on titles they do not hold at the start
     * of the campaign: a house dispossessed before 1471 presses its claim like one that lost the
     * title in play, so each is a clan-level conquest claim with the id that loss would mint.
     * </summary>
     */
    public IReadOnlyList<Claim> BuildInitialClaims()
    {
        var claims = new List<Claim>();
        foreach (string titleId in _allTitleIds)
        {
            foreach (FeudalClaimNode claim in _nodesByTitleId[titleId].InitialClaims ?? NoClaims)
            {
                claims.Add(new Claim(
                    $"{titleId}:{claim.ClanId}:conquest", claim.ClanId, titleId, claim.Strength, ClaimOrigin.Conquest));
            }
        }

        return claims;
    }

    private void IndexForest()
    {
        foreach (FeudalTitleNode rootNode in _forest)
        {
            IndexNode(rootNode, null);
        }
    }

    private void IndexNode(FeudalTitleNode node, string? parentTitleId)
    {
        _nodesByTitleId[node.TitleId] = node;
        _allTitleIds.Add(node.TitleId);
        if (parentTitleId is not null)
        {
            _parentTitleIdByTitleId[node.TitleId] = parentTitleId;
        }

        if (node.SeatSettlementId.Length > 0)
        {
            _titleIdBySeatSettlementId[node.SeatSettlementId] = node.TitleId;
        }

        var childTitleIds = new List<string>(node.Children.Count);
        foreach (FeudalTitleNode child in node.Children)
        {
            childTitleIds.Add(child.TitleId);
            IndexNode(child, node.TitleId);
        }

        _childTitleIdsByTitleId[node.TitleId] = childTitleIds;
    }
}
