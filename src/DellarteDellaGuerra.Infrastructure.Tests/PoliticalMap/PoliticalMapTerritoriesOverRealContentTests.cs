using System.Xml.Linq;
using DellarteDellaGuerra.Domain.PoliticalMap;
using DellarteDellaGuerra.Domain.PoliticalMap.Overlay;
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
        // 139 seats across three realms. Regions and shires are seatless duchy-tagged groups, so
        // no seat merges into another: every seat is its own territory, and the demesne fold
        // paints the ones that are not counties with a neighbour.
        IReadOnlyList<Title> seated = _titles.GetAllTitles().Where(title => title.SeatSettlementId.Length > 0).ToList();
        var territories = seated
            .Select(title => _territories.GetTerritoryOfSeat(title.SeatSettlementId))
            .ToList();

        Assert.Equal(139, seated.Count);
        Assert.DoesNotContain(null, territories);
        Assert.Equal(seated.Select(title => title.Id).OrderBy(id => id), territories.OrderBy(id => id));
    }

    [Theory]
    [Trait("Category", "DADG content integration")]
    [InlineData("Lancaster_town", "county_lancashire")]
    [InlineData("dadg_Piel_castle", "barony_piel")]
    [InlineData("dadg_Pontefract_castle", "duchy_lancaster")]
    public void ASeatIsPaintedWithItsCounty(string seat, string territory)
    {
        Assert.Equal(territory, _territories.GetTerritoryOfSeat(seat));
    }

    [Theory]
    [Trait("Category", "DADG content integration")]
    [InlineData("Pembroke_town", PoliticalMapOwnerLevel.Vassals, "region_march_of_wales", "March of Wales")]
    [InlineData("Pembroke_town", PoliticalMapOwnerLevel.SubVassals, "county_pembroke", "Earldom of Pembroke")]
    [InlineData("Exeter_town", PoliticalMapOwnerLevel.Vassals, "region_western_circuit", "Western Circuit")]
    [InlineData("Exeter_town", PoliticalMapOwnerLevel.SubVassals, "shire_devon", "Devon")]
    [InlineData("dadg_sco_dumfries_town", PoliticalMapOwnerLevel.Vassals, "region_scottish_marches", "Scottish Marches")]
    [InlineData("dadg_sco_dumfries_town", PoliticalMapOwnerLevel.SubVassals, "shire_dumfries", "Sheriffdom of Dumfries")]
    [InlineData("dadg_sco_hermitage_castle", PoliticalMapOwnerLevel.Vassals, "region_scottish_marches", "Scottish Marches")]
    [InlineData("dadg_sco_hermitage_castle", PoliticalMapOwnerLevel.SubVassals, "shire_liddesdale", "Lordship of Liddesdale")]
    [InlineData("dadg_sco_threave_castle", PoliticalMapOwnerLevel.SubVassals, "shire_galloway", "Lordship of Galloway")]
    public void GeographicLabelsNameTheirLocalTerritoryRatherThanTheHoldersDignity(
        string seat, PoliticalMapOwnerLevel level, string titleId, string name)
    {
        var painter = new PoliticalMapPainter(_structure, _titles, new IdentityGenealogy(), _territories);
        string key = painter.GeographicLabelKeyOf(seat, new GeographicWorld(), level);
        Assert.Equal(titleId, key);
        Assert.Equal(name, painter.GeographicLabelNameOf(key));
    }

    [Theory]
    [Trait("Category", "DADG content integration")]
    [InlineData("dadg_sco_dumfries_town", "clan_maxwell")]
    [InlineData("dadg_sco_caerlaverock_castle", "clan_maxwell")]
    [InlineData("dadg_sco_threave_castle", "clan_stewart")]
    [InlineData("dadg_sco_hermitage_castle", "clan_douglas_angus")]
    [InlineData("Pembroke_town", "clan_tudor")]
    // Reviewed independently of the XML; rationale and proxy limits: doc/starting-ownership-audit-1471.md.
    [InlineData("dadg_Pontefract_castle", "clan_lancaster")]
    [InlineData("Coventry_town", "clan_neville_of_middleham")]
    [InlineData("dadg_Kenilworth_castle", "clan_lancaster")]
    [InlineData("dadg_Barnard_castle", "clan_neville_of_middleham")]
    [InlineData("dadg_Penrith_castle", "clan_neville_of_middleham")]
    [InlineData("dadg_Skipton_castle", "clan_stanley")]
    [InlineData("dadg_Richmond_castle", "clan_york")]
    [InlineData("dadg_Helmsley_castle", "clan_york")]
    [InlineData("dadg_Tattershall_castle", "clan_bourchier")]
    [InlineData("dadg_Stafford_castle", "clan_stafford")]
    [InlineData("dadg_Corfe_castle", "clan_beaufort")]
    [InlineData("dadg_Farleigh_castle", "clan_york")]
    [InlineData("dadg_Wardour_castle", "clan_lancaster")]
    [InlineData("Exeter_town", "clan_courtenay")]
    [InlineData("dadg_Dunster_castle", "clan_herbert")]
    [InlineData("Launceston_town", "clan_lancaster")]
    [InlineData("dadg_Hertford_castle", "clan_lancaster")]
    [InlineData("dadg_Donnington_castle", "clan_de_la_pole")]
    [InlineData("dadg_Dunstanburgh_castle", "clan_lancaster")]
    [InlineData("dadg_sco_dunbar_castle", "clan_stewart")]
    [InlineData("dadg_sco_lochleven_castle", "clan_douglas_lochleven")]
    [InlineData("dadg_sco_kildrummy_castle", "clan_stewart")]
    [InlineData("dadg_sco_elgin_town", "clan_stewart")]
    [InlineData("dadg_sco_urquhart_castle", "clan_macdonald_isles")]
    [InlineData("dadg_sco_kirkevaag_village", "clan_stewart")]
    [InlineData("dadg_sco_dingwall_town", "clan_macdonald_isles")]
    public void StartingOwnersMatchTheirTitleHolders(string seat, string clan)
    {
        string dataRoot = new MapModuleContentProvider().MapDataRoot;
        XElement settlement = XDocument.Load(Path.Combine(dataRoot, "settlements.xml"))
            .Descendants("Settlement").Single(node => (string?)node.Attribute("id") == seat);
        Title title = _titles.GetAllTitles().Single(title => title.SeatSettlementId == seat);

        Assert.Equal("Faction." + clan, (string?)settlement.Attribute("owner"));
        Assert.Equal(clan, title.HolderHeroId);
    }

    [Fact]
    [Trait("Category", "DADG content integration")]
    public void StAndrewsKeepsItsLindsayGameplayOwnerAndCrownBishopricPlaceholder()
    {
        const string seat = "dadg_sco_st_andrews_town";
        string dataRoot = new MapModuleContentProvider().MapDataRoot;
        XElement settlement = XDocument.Load(Path.Combine(dataRoot, "settlements.xml"))
            .Descendants("Settlement").Single(node => (string?)node.Attribute("id") == seat);
        Title title = _titles.GetAllTitles().Single(title => title.SeatSettlementId == seat);

        Assert.Equal("Faction.clan_lindsay", (string?)settlement.Attribute("owner"));
        Assert.Equal("clan_stewart", title.HolderHeroId);
    }

    [Fact]
    [Trait("Category", "DADG content integration")]
    public void TheSeatlessEnglishCrownChangingHandsRepaintsNothing()
    {
        // The crown is seatless so that London, a county, survives a change of dynasty; a new
        // king leaves every seat's fill as it was.
        Assert.Empty(_territories.GetTerritoriesAffectedByTitle("kingdom_england"));
        Assert.Equal(new[] { "barony_piel" }, _territories.GetTerritoriesAffectedByTitle("barony_piel"));
    }

    private sealed class GeographicWorld : IPoliticalMapWorld
    {
        public string? SeatOf(string settlementId) => settlementId;
        public string? OwnerClanOf(string settlementId) => null;
        public string? FactionOf(string settlementId) => null;
        public uint? ClanColor(string clanId) => null;
        public uint? FactionColor(string factionId) => null;
        public string? KingdomOfClan(string clanId) => null;
        public string? RulerClanOf(string kingdomId) => null;
    }
}
