using System.Collections.Generic;
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
    private static readonly IReadOnlyList<string> NoVassals = new List<string>();

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

    public void Reattach(string titleId, string? suzerainTitleId)
    {
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

        foreach (KeyValuePair<string, string?> reattachment in reattachments)
        {
            if (!_nodesByTitleId.ContainsKey(reattachment.Key)) continue;
            if (reattachment.Value is not null && !_nodesByTitleId.ContainsKey(reattachment.Value)) continue;
            Reattach(reattachment.Key, reattachment.Value);
        }
    }

    /**
     * <summary>
     * Builds the initial titles from the configured hierarchy,
     * each held by the leader of its configured initial holder clan, if any,
     * since the configured year (day 0 when none is given).
     * </summary>
     */
    public IReadOnlyList<Title> BuildInitialTitles(IGenealogy genealogy)
    {
        var titles = new List<Title>(_allTitleIds.Count);
        foreach (string titleId in _allTitleIds)
        {
            FeudalTitleNode node = _nodesByTitleId[titleId];
            string? holderHeroId = node.InitialHolderClanId is { } clanId
                ? genealogy.GetClanLeaderId(clanId)
                : null;
            float heldSinceDay = (node.InitialHeldSinceYear ?? 0) * DaysInYear;
            titles.Add(new Title(
                node.TitleId, node.Name, node.Rank, node.SeatSettlementId, holderHeroId,
                heldSinceDay: heldSinceDay));
        }

        return titles;
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
