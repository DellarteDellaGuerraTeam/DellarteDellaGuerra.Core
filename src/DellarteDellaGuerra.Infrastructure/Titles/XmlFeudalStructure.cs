using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Titles.Model;
using DellarteDellaGuerra.Domain.Titles.Port;
using DellarteDellaGuerra.Infrastructure.Titles.Model;

namespace DellarteDellaGuerra.Infrastructure.Titles;

/**
 * <summary>
 * The static de jure feudal hierarchy backed by the forest parsed from the titles configuration XML.
 * </summary>
 */
public class XmlFeudalStructure : IFeudalStructure
{
    private static readonly IReadOnlyList<string> NoVassals = new List<string>();

    private readonly Dictionary<string, FeudalTitleNode> _nodesByTitleId = new Dictionary<string, FeudalTitleNode>();
    private readonly Dictionary<string, string> _parentTitleIdByTitleId = new Dictionary<string, string>();
    private readonly Dictionary<string, string> _titleIdBySeatSettlementId = new Dictionary<string, string>();
    private readonly Dictionary<string, IReadOnlyList<string>> _childTitleIdsByTitleId =
        new Dictionary<string, IReadOnlyList<string>>();
    private readonly List<string> _allTitleIds = new List<string>();

    internal XmlFeudalStructure(IReadOnlyList<FeudalTitleNode> forest)
    {
        foreach (FeudalTitleNode rootNode in forest)
        {
            IndexNode(rootNode, null);
        }
    }

    public string? GetDeJureSuzerainTitleId(string titleId)
    {
        return _parentTitleIdByTitleId.TryGetValue(titleId, out string parentTitleId) ? parentTitleId : null;
    }

    public IReadOnlyList<string> GetDeJureVassalTitleIds(string titleId)
    {
        return _childTitleIdsByTitleId.TryGetValue(titleId, out IReadOnlyList<string> childTitleIds)
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

    /**
     * <summary>
     * Builds the initial titles from the configured hierarchy,
     * each held by its configured initial holder clan, if any.
     * </summary>
     */
    public IReadOnlyList<Title> BuildInitialTitles()
    {
        var titles = new List<Title>(_allTitleIds.Count);
        foreach (string titleId in _allTitleIds)
        {
            FeudalTitleNode node = _nodesByTitleId[titleId];
            titles.Add(new Title(node.TitleId, node.Name, node.Rank, node.SeatSettlementId, node.InitialHolderClanId));
        }

        return titles;
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
