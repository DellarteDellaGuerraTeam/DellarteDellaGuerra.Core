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
    public HeroNode? GetHero(string heroId)
    {
        var hero = TaleWorlds.CampaignSystem.Campaign.Current?.CampaignObjectManager.Find<Hero>(heroId);
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
        => TaleWorlds.CampaignSystem.Campaign.Current?.CampaignObjectManager.Find<Hero>(heroId)?.Clan?.StringId;
}
