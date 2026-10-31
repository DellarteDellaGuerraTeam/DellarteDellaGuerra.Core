using System.Xml.Linq;
using DellarteDellaGuerra.Domain.PoliticalMap;
using DellarteDellaGuerra.Domain.PoliticalMap.Grid;
using DellarteDellaGuerra.Domain.PoliticalMap.Overlay;
using DellarteDellaGuerra.Infrastructure.PoliticalMap;
using DellarteDellaGuerra.Infrastructure.Tests.Titles;
using DellarteDellaGuerra.Infrastructure.Titles;
using Xunit.Abstractions;

namespace DellarteDellaGuerra.Infrastructure.Tests.PoliticalMap;

/// <summary>
/// Runs the political map overlay's domain over the real 1471 map, titles and starting owners:
/// the demesne folds the grid yields, live owner regions and their mesh size.
/// </summary>
public class PoliticalMapOverlayOverRealContentTests
{
    private static readonly Lazy<Fixture> Built = new(Build);

    private readonly ITestOutputHelper _output;

    public PoliticalMapOverlayOverRealContentTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    [Trait("Category", "DADG content integration")]
    public void EachDuchyDemesneFoldsIntoTheCountyItBordersMost()
    {
        // The same folds tools/political_map_bake.py draws: Pontefract into York, Warwick into
        // Oxford, Fotheringhay into Warwick, Stafford into Cheshire, Framlingham into Norwich,
        // Corfe into Bristol and Harlech into Gwynedd.
        Assert.Equal(
            new Dictionary<string, string>
            {
                ["duchy_buckingham"] = "county_cheshire",
                ["duchy_lancaster"] = "county_york",
                ["duchy_norfolk"] = "county_norwich",
                ["duchy_somerset"] = "county_bristol",
                ["duchy_wales"] = "county_gwynedd",
                ["duchy_warwick"] = "county_oxford",
                ["duchy_york"] = "county_warwick"
            },
            Built.Value.Folds);
    }

    [Fact]
    [Trait("Category", "DADG content integration")]
    public void TheOwnersViewMeshesTheWholeMapInAFewThousandQuads()
    {
        Fixture fixture = Built.Value;
        PoliticalMapSettlementPaint[] paints = fixture.Painter.Paint(
            PoliticalMapView.Owners, fixture.Grid.SettlementIds, fixture.Territories, fixture.Folds, fixture.World);
        var mesher = new PoliticalMapMesher(fixture.Grid, fixture.Territories);

        int quads = 0, chunks = 0;
        foreach (string territory in mesher.Territories)
        {
            IReadOnlyList<PoliticalMapMeshChunk> built = mesher.Build(territory, paints);
            chunks += built.Count;
            quads += built.Sum(chunk => chunk.Quads.Count);
        }

        // About 20,000 quads in 54 territories (2026-10-30, block-aligned): some half a second of mesh building at
        // 9 µs a triangle, which the scheduler spreads over a second or two of frames.
        _output.WriteLine($"{mesher.Territories.Count()} territories, {chunks} chunks, {quads} quads");
        Assert.Equal(54, mesher.Territories.Count());
        Assert.InRange(quads, 1, 40_000);
    }

    [Fact]
    [Trait("Category", "DADG content integration")]
    public void TheOwnersViewPaintsEachLiveOwnerRegionOneFlatColour()
    {
        Fixture fixture = Built.Value;
        IReadOnlyList<(int A, int B)> neighbours = PoliticalMapSettlementAdjacency.Of(fixture.Grid);
        PoliticalMapSettlementPaint[] paints = fixture.Painter.Paint(
            PoliticalMapView.Owners, fixture.Grid.SettlementIds, fixture.Territories, fixture.Folds, fixture.World, neighbours);

        foreach (IGrouping<string?, PoliticalMapSettlementPaint> region in paints.Where(paint => paint.Fill != 0).GroupBy(paint => paint.RegionKey))
        {
            Assert.NotNull(region.Key);
            Assert.Single(region.Select(paint => paint.Fill).Distinct());
        }

        int touching = 0;
        foreach ((int a, int b) in neighbours)
        {
            PoliticalMapSettlementPaint first = paints[a], second = paints[b];
            if (first.Fill == 0 || second.Fill == 0 || first.RegionKey == second.RegionKey) continue;

            touching++;
        }

        int regions = paints.Where(paint => paint.Fill != 0).Select(paint => paint.RegionKey).Distinct().Count();
        _output.WriteLine($"{regions} regions, {touching} touching region pairs");
        Assert.True(touching > 0);
    }

    [Fact]
    [Trait("Category", "DADG content integration")]
    public void KingdomlessOwnersKeepTheirOwnRegionsAtBothZoomLevels()
    {
        Fixture fixture = Built.Value;
        foreach (PoliticalMapOwnerLevel level in Enum.GetValues<PoliticalMapOwnerLevel>())
        {
            PoliticalMapSettlementPaint[] paints = fixture.Painter.Paint(
                PoliticalMapView.Owners, fixture.Grid.SettlementIds, fixture.Territories, fixture.Folds, fixture.World, level: level);
            for (int index = 0; index < paints.Length; index++)
                Assert.Equal(fixture.World.OwnerClanOf(fixture.Grid.SettlementIds[index]), paints[index].RegionKey);
        }
    }

    private sealed record Fixture(
        PoliticalMapGrid Grid,
        PoliticalMapPainter Painter,
        ContentWorld World,
        string[] Territories,
        IReadOnlyDictionary<string, string> Folds);

    private static Fixture Build()
    {
        using Stream titlesStream = DadgContent.OpenTitles();
        var structure = new XmlFeudalStructure(FeudalStructureParser.Parse(titlesStream));
        var titles = new InMemoryTitleRegistry(new IdentityGenealogy());
        titles.Initialise(structure.BuildInitialTitles(new IdentityGenealogy()));
        var painter = new PoliticalMapPainter(structure, titles, new IdentityGenealogy(), new PoliticalMapTerritories(structure, titles));

        string mapModule = Path.GetDirectoryName(new MapModuleContentProvider().MapDataRoot)!;
        List<XElement> settlements = XDocument.Load(Path.Combine(mapModule, "ModuleData", "settlements.xml"))
            .Descendants("Settlement")
            .Where(settlement => settlement.Element("Components")?.Elements().FirstOrDefault()?.Name.LocalName is "Town" or "Village")
            .ToList();
        var seats = settlements.ToDictionary(
            settlement => (string)settlement.Attribute("id")!,
            settlement => ((string?)settlement.Element("Components")!.Elements().First().Attribute("bound"))?.Replace("Settlement.", "")
                          ?? (string)settlement.Attribute("id")!);
        IReadOnlyList<PoliticalMapSeed> seeds = settlements
            .Select(settlement => new PoliticalMapSeed(
                (string)settlement.Attribute("id")!, (float)settlement.Attribute("posX")!, (float)settlement.Attribute("posY")!))
            .ToList();
        PoliticalMapGrid grid = PoliticalMapGridBuilder.Build(MainMapTerrain.Load(Path.Combine(mapModule, "SceneObj", "Main_map")), seeds);

        var owners = settlements
            .Where(settlement => settlement.Attribute("owner") is not null)
            .ToDictionary(settlement => (string)settlement.Attribute("id")!, settlement => ((string)settlement.Attribute("owner")!).Replace("Faction.", ""));

        var world = new ContentWorld(seats, owners);
        string[] territories = grid.SettlementIds.Select(id => painter.TerritoryOf(id, world)).ToArray();
        return new Fixture(grid, painter, world, territories, painter.Fold(grid, territories));
    }

    // The starting owners from settlements.xml (a village answers to its bound seat's owner); no factions or colours.
    private sealed class ContentWorld : IPoliticalMapWorld
    {
        private readonly IReadOnlyDictionary<string, string> _seats;
        private readonly IReadOnlyDictionary<string, string> _owners;

        public ContentWorld(IReadOnlyDictionary<string, string> seats, IReadOnlyDictionary<string, string> owners)
        {
            _seats = seats;
            _owners = owners;
        }

        public string? SeatOf(string settlementId) => _seats.TryGetValue(settlementId, out string? seat) ? seat : settlementId;

        public string? OwnerClanOf(string settlementId) =>
            _owners.TryGetValue(SeatOf(settlementId)!, out string? owner) || _owners.TryGetValue(settlementId, out owner) ? owner : null;

        public string? FactionOf(string settlementId) => null;

        public uint? ClanColor(string clanId) => (uint)clanId.GetHashCode() | 0xFF000000;

        public uint? FactionColor(string factionId) => null;

        public string? KingdomOfClan(string clanId) => null;

        public string? RulerClanOf(string kingdomId) => null;
    }
}
