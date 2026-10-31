using DellarteDellaGuerra.Domain.PoliticalMap;
using DellarteDellaGuerra.Domain.Titles.Model;
using DellarteDellaGuerra.Infrastructure.Tests.Titles;
using DellarteDellaGuerra.Infrastructure.Titles;

namespace DellarteDellaGuerra.Infrastructure.Tests.PoliticalMap;

/// <summary>
/// Groups the real 1471 seats into the political map's territories. The overlay builds and
/// repaints its meshes per territory, so this is the set a holder change is resolved against.
/// </summary>
public class PoliticalMapTerritoriesOverRealContentTests
{
    private readonly XmlFeudalStructure _structure;
    private readonly InMemoryTitleRegistry _titles;
    private readonly PoliticalMapTerritories _territories;

    public PoliticalMapTerritoriesOverRealContentTests()
    {
        using Stream titlesStream = DadgContent.OpenTitles();
        _structure = new XmlFeudalStructure(FeudalStructureParser.Parse(titlesStream));
        _titles = new InMemoryTitleRegistry(new IdentityGenealogy());
        _titles.Initialise(_structure.BuildInitialTitles(new IdentityGenealogy()));
        _territories = new PoliticalMapTerritories(_structure, _titles);
    }

    [Fact]
    [Trait("Category", "DADG content integration")]
    public void EverySeatBelongsToACountyOrToTheDuchyItSeats()
    {
        // 139 seats across three realms: every town seats a county and every barony lies in one,
        // so the only territories that are not counties are the seven English duchies seated at
        // castles of their own. A barony left outside a county would show up here as a territory.
        IReadOnlyList<Title> seated = _titles.GetAllTitles().Where(title => title.SeatSettlementId.Length > 0).ToList();
        var territories = seated
            .Select(title => _territories.GetTerritoryOfSeat(title.SeatSettlementId))
            .ToList();

        Assert.Equal(139, seated.Count);
        Assert.DoesNotContain(null, territories);
        Assert.Equal(54, territories.Distinct().Count());
        Assert.Equal(
            new[]
            {
                "duchy_buckingham", "duchy_lancaster", "duchy_norfolk", "duchy_somerset",
                "duchy_wales", "duchy_warwick", "duchy_york"
            },
            territories.Distinct().Where(territory => _structure.GetRank(territory!) != TitleRank.Count).OrderBy(id => id));
    }

    [Theory]
    [Trait("Category", "DADG content integration")]
    [InlineData("Lancaster_town", "county_lancashire")]
    [InlineData("dadg_Piel_castle", "county_lancashire")]
    [InlineData("dadg_Pontefract_castle", "duchy_lancaster")]
    public void ASeatIsPaintedWithItsCounty(string seat, string territory)
    {
        Assert.Equal(territory, _territories.GetTerritoryOfSeat(seat));
    }

    [Fact]
    [Trait("Category", "DADG content integration")]
    public void TheSeatlessEnglishCrownChangingHandsRepaintsNothing()
    {
        // The crown is seatless so that London, a county, survives a change of dynasty; a new
        // king leaves every county's fill as it was.
        Assert.Empty(_territories.GetTerritoriesAffectedByTitle("kingdom_england"));
        Assert.Equal(new[] { "county_lancashire" }, _territories.GetTerritoriesAffectedByTitle("barony_piel"));
    }
}
