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
        // Every seat is its own territory, so every non-county seat (a duchy seated at a castle or
        // a barony) is painted as the county it borders most, the same way
        // tools/political_map_bake.py draws it.
        Assert.Equal(
            new Dictionary<string, string>
            {
                ["barony_arundel"] = "county_arundel",
                ["barony_baconsthorpe"] = "county_howard",
                ["barony_balnagowan"] = "county_sutherland",
                ["barony_bamburgh"] = "county_march",
                ["barony_barnard"] = "county_lancashire",
                ["barony_beaumaris"] = "county_ferrers",
                ["barony_beeston"] = "county_rivers",
                ["barony_berkhamsted"] = "county_oxford",
                ["barony_brecon"] = "county_bergavenny",
                ["barony_caerlaverock"] = "county_maxwell",
                ["barony_caerphilly"] = "county_glamorgan",
                ["barony_cardigan"] = "county_pembroke",
                ["barony_carisbrooke"] = "county_hampshire",
                ["barony_carmarthen"] = "county_ceredigion",
                ["barony_cawdor"] = "county_westfield",
                ["barony_chepstow"] = "county_bristol",
                ["barony_chirk"] = "county_shrewsbury",
                ["barony_clare"] = "county_suffolk",
                ["barony_conisbrough"] = "county_lincoln",
                ["barony_conwy"] = "county_ferrers",
                ["barony_denbigh"] = "county_rivers",
                ["barony_dirlot"] = "county_sutherland",
                ["barony_donnington"] = "county_oxford",
                ["barony_dover"] = "county_kent",
                ["barony_duart"] = "county_argyll",
                ["barony_dunbar"] = "county_march",
                ["barony_dunnottar"] = "county_crawford",
                ["barony_dunstanburgh"] = "county_northumberland",
                ["barony_dunster"] = "county_devon",
                ["barony_dunvegan"] = "county_lewis",
                ["barony_dunyvaig"] = "county_islay",
                ["barony_durham"] = "county_westmorland",
                ["barony_farleigh"] = "county_bristol",
                ["barony_farnham"] = "county_arundel",
                ["barony_foulis"] = "county_ross",
                ["barony_gloucester"] = "county_bristol",
                ["barony_hedingham"] = "county_essex",
                ["barony_helmsley"] = "county_york",
                ["barony_angus"] = "county_clifford",
                ["barony_hertford"] = "county_london",
                ["barony_kendal"] = "county_clifford",
                ["barony_kenilworth"] = "county_warwick",
                ["barony_kintail"] = "county_ross",
                ["barony_knapdale"] = "county_argyll",
                ["barony_lewes"] = "county_arundel",
                ["barony_liverpool"] = "county_rivers",
                ["barony_lochleven"] = "county_lothian",
                ["barony_ludlow"] = "county_bergavenny",
                ["barony_mar"] = "county_ruthven",
                ["barony_middleham"] = "county_york",
                ["barony_moidart"] = "county_lochiel",
                ["barony_montgomery"] = "county_shrewsbury",
                ["barony_northampton"] = "county_warwick",
                ["barony_nottingham"] = "county_warwick",
                ["barony_okehampton"] = "county_cornwall",
                ["barony_painscastle"] = "county_ceredigion",
                ["barony_penrith"] = "county_clifford",
                ["barony_peveril"] = "county_hallamshire",
                ["barony_piel"] = "county_clifford",
                ["barony_plymouth"] = "county_cornwall",
                ["barony_rochester"] = "county_kent",
                ["barony_rothesay"] = "county_argyll",
                ["barony_rye"] = "county_kent",
                ["barony_saint_michaels_mount"] = "county_cornwall",
                ["barony_scarborough"] = "county_hull",
                ["barony_exeter"] = "county_devon",
                ["barony_skipton"] = "county_lancashire",
                ["barony_huntly"] = "county_marischal",
                ["barony_swansea"] = "county_glamorgan",
                ["barony_tattershall"] = "county_lincoln",
                ["barony_taunton"] = "county_devon",
                ["barony_threave"] = "county_kennedy",
                ["barony_tonbridge"] = "county_london",
                ["barony_totnes"] = "county_devon",
                ["barony_urquhart"] = "county_lochiel",
                ["barony_wardour"] = "county_hampshire",
                ["barony_winchester"] = "county_hampshire",
                ["barony_windsor"] = "county_london",
                ["barony_worcester"] = "county_warwick",
                ["duchy_buckingham"] = "county_shrewsbury",
                ["duchy_lancaster"] = "county_york",
                ["duchy_norfolk"] = "county_howard",
                ["duchy_somerset"] = "county_hampshire",
                ["duchy_wales"] = "county_ferrers",
                ["duchy_warwick"] = "county_oxford",
                ["duchy_york"] = "county_lynn"
            },
            Built.Value.Folds);
    }

    [Fact]
    [Trait("Category", "DADG content integration")]
    public void TheOwnersViewMeshesTheWholeMapWithSmoothFrontiersWithinTheTerrainAndTriangleBudgets()
    {
        Fixture fixture = Built.Value;
        PoliticalMapSettlementPaint[] paints = fixture.Painter.Paint(
            PoliticalMapView.Owners, fixture.Grid.SettlementIds, fixture.Territories, fixture.Folds, fixture.World);
        var mesher = new PoliticalMapMesher(fixture.Grid, fixture.Territories);
        var sheet = new PoliticalMapSheet(fixture.Terrain, fixture.Grid);
        var watch = System.Diagnostics.Stopwatch.StartNew();

        int quads = 0, chunks = 0, triangles = 0;
        foreach (string territory in mesher.Territories)
        {
            IReadOnlyList<PoliticalMapMeshChunk> built = mesher.Build(territory, paints);
            chunks += built.Count;
            quads += built.Sum(chunk => chunk.Quads.Count);
            triangles += built.Sum(chunk => chunk.TriangleCount);
            Assert.All(built, chunk => Assert.InRange(chunk.TriangleCount, 1, PoliticalMapMesher.MaxTrianglesPerChunk));
            foreach (PoliticalMapTriangle triangle in built.SelectMany(chunk => chunk.Triangles))
            {
                PoliticalMapPoint a = triangle.A, b = triangle.B, c = triangle.C;
                Assert.True((b.Column - a.Column) * (c.Row - a.Row) - (b.Row - a.Row) * (c.Column - a.Column) > 0,
                    $"Non-positive triangle in {territory}: {a}, {b}, {c}");
                float minX = Math.Min(a.Column, Math.Min(b.Column, c.Column)), maxX = Math.Max(a.Column, Math.Max(b.Column, c.Column));
                float minY = Math.Min(a.Row, Math.Min(b.Row, c.Row)), maxY = Math.Max(a.Row, Math.Max(b.Row, c.Row));
                Assert.True(float.IsFinite(minX) && float.IsFinite(maxX) && float.IsFinite(minY) && float.IsFinite(maxY));
                // A block's sheet corners are all above that block's maximum terrain height.
                Assert.True(maxX <= (Math.Floor(minX / 8) + 1) * 8 + 0.00001, $"Triangle crosses sheet column in {territory}: {minX}..{maxX}");
                Assert.True(maxY <= (Math.Floor(minY / 8) + 1) * 8 + 0.00001, $"Triangle crosses sheet row in {territory}: {minY}..{maxY}");
                foreach ((float wa, float wb, float wc) in new[] { (0.333f, 0.333f, 0.334f), (0.7f, 0.1f, 0.2f) })
                {
                    float x = a.Column * wa + b.Column * wb + c.Column * wc;
                    float y = a.Row * wa + b.Row * wb + c.Row * wc;
                    float height = sheet.HeightAt(a.Column, a.Row) * wa + sheet.HeightAt(b.Column, b.Row) * wb + sheet.HeightAt(c.Column, c.Row) * wc;
                    Assert.True(height + 0.0001f >= fixture.Terrain.HeightAt(x * fixture.Grid.CellSize, y * fixture.Grid.CellSize),
                        $"Triangle falls below terrain in {territory} at {x},{y}");
                }
            }
        }

        _output.WriteLine($"{mesher.Territories.Count()} territories, {chunks} chunks, {quads} quads, {triangles} triangles; planning and geometry checks {watch.ElapsedMilliseconds} ms");
        Assert.Equal(139, mesher.Territories.Count());
        Assert.InRange(quads, 1, 40_000);
        Assert.InRange(triangles, 1, 250_000);
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
        PoliticalMapHeightField Terrain,
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
        PoliticalMapHeightField terrain = MainMapTerrain.Load(Path.Combine(mapModule, "SceneObj", "Main_map"));
        PoliticalMapGrid grid = PoliticalMapGridBuilder.Build(terrain, seeds);

        var owners = settlements
            .Where(settlement => settlement.Attribute("owner") is not null)
            .ToDictionary(settlement => (string)settlement.Attribute("id")!, settlement => ((string)settlement.Attribute("owner")!).Replace("Faction.", ""));

        var world = new ContentWorld(seats, owners);
        string[] territories = grid.SettlementIds.Select(id => painter.TerritoryOf(id, world)).ToArray();
        return new Fixture(grid, terrain, painter, world, territories, painter.Fold(grid, territories));
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
