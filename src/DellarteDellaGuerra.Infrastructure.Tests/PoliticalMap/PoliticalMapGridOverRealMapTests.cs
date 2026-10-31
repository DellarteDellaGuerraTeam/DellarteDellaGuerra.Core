using System.Diagnostics;
using System.Xml.Linq;
using DellarteDellaGuerra.Domain.PoliticalMap.Grid;
using DellarteDellaGuerra.Infrastructure.PoliticalMap;
using DellarteDellaGuerra.Infrastructure.Tests.Titles;
using Xunit.Abstractions;

namespace DellarteDellaGuerra.Infrastructure.Tests.PoliticalMap;

/// <summary>
/// Builds the political map's settlement grid from the real 1471 map: the terrain.bin and
/// scene.xscene of the sibling DellarteDellaGuerraMap module, read the way the campaign reads them
/// at load, seeded from its settlements.xml. These files are too large to embed, so the tests
/// need the module.
/// </summary>
/// <remarks>
/// Checked once by hand (2026-10-30) against the grid tools/political_map_bake.py bakes from the
/// same files: the land cells are identical (256,919) and 99.76% of them have the same owner. The
/// 621 that differ all lie on a border between two settlements, where two paths over land tie and
/// the two Dijkstra implementations break the tie differently.
/// </remarks>
public class PoliticalMapGridOverRealMapTests
{
    private static readonly Lazy<(PoliticalMapGrid Grid, long TerrainMs, long BuildMs)> Built = new(BuildFromMapModule);

    private readonly ITestOutputHelper _output;

    public PoliticalMapGridOverRealMapTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    [Trait("Category", "DADG content integration")]
    public void TheMapTerrainDecodesToA2049SampleHeightmapOverThe1120MeterMap()
    {
        PoliticalMapHeightField terrain = MainMapTerrain.Load(SceneFolder());

        Assert.Equal(2049, terrain.Side);
        Assert.Equal(1120f, terrain.MapSize);
        Assert.Equal(0f, terrain.MinHeight);
        Assert.Equal(15.686f, terrain.MaxHeight, 3);
    }

    [Fact]
    [Trait("Category", "DADG content integration")]
    public void TheLandOfTheMapIsSharedOutAndTheSeaIsNot()
    {
        (PoliticalMapGrid grid, long terrainMs, long buildMs) = Built.Value;
        _output.WriteLine($"terrain read in {terrainMs} ms, grid built in {buildMs} ms");

        Assert.Equal(1120, grid.Width);
        Assert.Equal(1120, grid.Height);
        // About a fifth of the map is land a settlement can reach; the rest is sea and Ireland.
        Assert.InRange(grid.Cells.Count(cell => cell != PoliticalMapGrid.None), 250_000, 265_000);
    }

    [Theory]
    [Trait("Category", "DADG content integration")]
    [InlineData("Lancaster_town")]
    [InlineData("dadg_Piel_castle")]
    public void ASettlementOwnsTheCellItStandsOn(string settlementId)
    {
        PoliticalMapGrid grid = Built.Value.Grid;
        PoliticalMapSeed seed = ReadSeeds().Single(candidate => candidate.SettlementId == settlementId);

        Assert.Equal(settlementId, grid.SettlementAt(seed.X, seed.Y));
    }

    [Fact]
    [Trait("Category", "DADG content integration")]
    public void LondonStandsInTheThamesYetHoldsTheBanksAroundIt()
    {
        PoliticalMapGrid grid = Built.Value.Grid;
        PoliticalMapSeed london = ReadSeeds().Single(candidate => candidate.SettlementId == "town_V6");
        int index = grid.SettlementIds.ToList().IndexOf("town_V6") + 1;

        Assert.Null(grid.SettlementAt(london.X, london.Y));
        Assert.Contains((ushort)index, grid.Cells);
    }

    private static (PoliticalMapGrid, long, long) BuildFromMapModule()
    {
        var clock = Stopwatch.StartNew();
        PoliticalMapHeightField terrain = MainMapTerrain.Load(SceneFolder());
        long terrainMs = clock.ElapsedMilliseconds;
        IReadOnlyList<PoliticalMapSeed> seeds = ReadSeeds();
        clock.Restart();
        PoliticalMapGrid grid = PoliticalMapGridBuilder.Build(terrain, seeds);
        return (grid, terrainMs, clock.ElapsedMilliseconds);
    }

    // The towns, castles and villages in file order, as the campaign seeds them from Settlement.All.
    private static IReadOnlyList<PoliticalMapSeed> ReadSeeds() =>
        XDocument.Load(Path.Combine(MapModule(), "ModuleData", "settlements.xml"))
            .Descendants("Settlement")
            .Where(settlement => settlement.Element("Components")?.Elements().FirstOrDefault()?.Name.LocalName is "Town" or "Village")
            .Select(settlement => new PoliticalMapSeed(
                (string)settlement.Attribute("id")!,
                (float)settlement.Attribute("posX")!,
                (float)settlement.Attribute("posY")!))
            .ToList();

    private static string SceneFolder() => Path.Combine(MapModule(), "SceneObj", "Main_map");

    private static string MapModule() => Path.GetDirectoryName(new MapModuleContentProvider().MapDataRoot)!;
}
