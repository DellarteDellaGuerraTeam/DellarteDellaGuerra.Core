using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.Titles.Model;
using DellarteDellaGuerra.Domain.Titles.Port;

namespace DellarteDellaGuerra.Infrastructure.Titles;

/**
 * <summary>
 * An in-memory registry of the feudal titles and their current holders.
 * <br/>
 * <br/>
 * The campaign tick is single-threaded, so plain dictionaries are sufficient.
 * </summary>
 */
public class InMemoryTitleRegistry : ITitleRepository
{
    private readonly IGenealogy _genealogy;
    private readonly Dictionary<string, Title> _titlesByTitleId = new Dictionary<string, Title>();
    private readonly Dictionary<string, string> _titleIdBySeatSettlementId = new Dictionary<string, string>();
    private readonly Dictionary<string, string> _primaryTitleIdByClanId = new Dictionary<string, string>();

    public InMemoryTitleRegistry(IGenealogy genealogy)
    {
        _genealogy = genealogy;
    }

    /**
     * <summary>
     * Clears the registry and loads the given titles, typically on campaign start or save load.
     * </summary>
     */
    public void Initialise(IEnumerable<Title> titles)
    {
        _titlesByTitleId.Clear();
        _titleIdBySeatSettlementId.Clear();
        foreach (Title title in titles)
        {
            SaveTitle(title);
        }
    }

    /**
     * <summary>
     * Returns a copy of all the registered titles, typically for save game serialisation.
     * </summary>
     */
    public IReadOnlyList<Title> Snapshot()
    {
        return _titlesByTitleId.Values.ToList();
    }

    /**
     * <summary>
     * Replaces the primary titles the clans were found to serve under, typically on campaign start or save load.
     * </summary>
     */
    public void InitialisePrimaryTitles(IReadOnlyDictionary<string, string> primaryTitleIdByClanId)
    {
        _primaryTitleIdByClanId.Clear();
        foreach (KeyValuePair<string, string> primaryTitle in primaryTitleIdByClanId)
        {
            _primaryTitleIdByClanId[primaryTitle.Key] = primaryTitle.Value;
        }
    }

    /**
     * <summary>
     * Returns a copy of the primary titles the clans were found to serve under, typically for save game serialisation.
     * </summary>
     */
    public IReadOnlyDictionary<string, string> SnapshotPrimaryTitles()
    {
        return new Dictionary<string, string>(_primaryTitleIdByClanId);
    }

    public string? GetPrimaryTitleId(string clanId)
    {
        return _primaryTitleIdByClanId.TryGetValue(clanId, out string titleId) ? titleId : null;
    }

    public void SavePrimaryTitleId(string clanId, string titleId)
    {
        _primaryTitleIdByClanId[clanId] = titleId;
    }

    public Title? GetTitle(string titleId)
    {
        return _titlesByTitleId.TryGetValue(titleId, out Title title) ? title : null;
    }

    public Title? GetTitleBySeat(string settlementId)
    {
        return _titleIdBySeatSettlementId.TryGetValue(settlementId, out string titleId)
            ? GetTitle(titleId)
            : null;
    }

    public IReadOnlyList<Title> GetTitlesByClan(string clanId)
    {
        return _titlesByTitleId.Values.Where(title => _genealogy.GetHolderClanOf(title) == clanId).ToList();
    }

    public IReadOnlyList<Title> GetAllTitles()
    {
        return _titlesByTitleId.Values.ToList();
    }

    public void SaveTitle(Title title)
    {
        if (_titlesByTitleId.TryGetValue(title.Id, out Title existingTitle)
            && existingTitle.SeatSettlementId.Length > 0
            && existingTitle.SeatSettlementId != title.SeatSettlementId)
        {
            _titleIdBySeatSettlementId.Remove(existingTitle.SeatSettlementId);
        }

        _titlesByTitleId[title.Id] = title;
        if (title.SeatSettlementId.Length > 0)
        {
            _titleIdBySeatSettlementId[title.SeatSettlementId] = title.Id;
        }
    }
}
