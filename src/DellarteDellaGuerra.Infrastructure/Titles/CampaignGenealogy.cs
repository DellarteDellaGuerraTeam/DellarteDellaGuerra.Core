using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.Titles.Model;
using DellarteDellaGuerra.Domain.Titles.Port;
using TaleWorlds.CampaignSystem;
using TaleWorlds.ObjectSystem;

namespace DellarteDellaGuerra.Infrastructure.Titles;

/**
 * <summary>
 * Reads the bloodlines claim derivation needs straight from the live campaign.
 * </summary>
 */
public class CampaignGenealogy : IGenealogy
{
    public HeroNode? GetHero(string heroId)
    {
        var hero = MBObjectManager.Instance?.GetObject<Hero>(heroId);
        if (hero is null) return null;

        return new HeroNode(
            hero.StringId,
            hero.IsFemale,
            hero.IsAlive,
            hero.Clan?.StringId,
            hero.Children?.Select(child => child.StringId).ToList() ?? new List<string>());
    }

    public string? GetClanLeaderId(string clanId)
    {
        return MBObjectManager.Instance?.GetObject<Clan>(clanId)?.Leader?.StringId;
    }

    public string? GetClanOf(string heroId)
        => MBObjectManager.Instance?.GetObject<Hero>(heroId)?.Clan?.StringId;

    public IReadOnlyList<string> GetDeceasedClanMemberIds(string clanId)
    {
        var clan = MBObjectManager.Instance?.GetObject<Clan>(clanId);
        if (clan is null || TaleWorlds.CampaignSystem.Campaign.Current is null) return new List<string>();

        return TaleWorlds.CampaignSystem.Campaign.Current.DeadOrDisabledHeroes
            .Where(hero => hero.Clan == clan)
            .Select(hero => hero.StringId)
            .ToList();
    }
}
