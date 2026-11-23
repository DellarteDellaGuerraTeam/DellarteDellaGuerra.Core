using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.Titles.Model;
using DellarteDellaGuerra.Domain.Titles.Port;
using TaleWorlds.CampaignSystem;

namespace DellarteDellaGuerra.Infrastructure.Titles;

/**
 * <summary>
 * Reads the bloodlines claim derivation needs straight from the live campaign.
 * </summary>
 */
public class CampaignGenealogy : IGenealogy
{
    private readonly ConcurrentDictionary<string, Hero> _heroesById = new();
    private TaleWorlds.CampaignSystem.Campaign? _heroesCampaign;

    public HeroNode? GetHero(string heroId)
    {
        var hero = FindHero(heroId);
        if (hero is null) return null;

        return new HeroNode(
            hero.StringId,
            hero.IsFemale,
            hero.IsAlive,
            hero.Clan?.StringId,
            hero.Children?.Select(child => child.StringId).ToList() ?? new List<string>(),
            hero.Father?.StringId,
            hero.Age,
            hero.Spouse?.StringId);
    }

    public string? GetClanLeaderId(string clanId)
    {
        return TaleWorlds.CampaignSystem.Campaign.Current?.CampaignObjectManager.Find<Clan>(clanId)?.Leader?.StringId;
    }

    public string? GetClanOf(string heroId)
        => FindHero(heroId)?.Clan?.StringId;

    // CampaignObjectManager.Find scans every dead and alive hero, and title queries resolve a holder per
    // title. A found hero is reused while the manager still lists it: only UnregisterDeadHero drops a
    // hero, and it clears IsReady, so a cleared flag falls back to the scan.
    private Hero? FindHero(string heroId)
    {
        var campaign = TaleWorlds.CampaignSystem.Campaign.Current;
        if (campaign is null) return null;
        if (!ReferenceEquals(campaign, _heroesCampaign))
        {
            _heroesById.Clear();
            _heroesCampaign = campaign;
        }

        if (_heroesById.TryGetValue(heroId, out var hero) && hero.IsReady) return hero;

        hero = campaign.CampaignObjectManager.Find<Hero>(heroId);
        if (hero is null) _heroesById.TryRemove(heroId, out _);
        else _heroesById[heroId] = hero;
        return hero;
    }
}
