using DellarteDellaGuerra.Domain.PoliticalMap.Grid;
using DellarteDellaGuerra.Domain.PoliticalMap.Overlay;

namespace DellarteDellaGuerra.Domain.Tests.PoliticalMap.Overlay;

public class PoliticalMapFrontierStrokeTests
{
    private static PoliticalMapSettlementPaint Paint(string region, string? parent = null) =>
        new(0x99CC5555, 0, null, null, null, region) { ParentRegionKey = parent };

    private static PoliticalMapGrid Grid(int height = 12) => new(6, height, 1, new[] { "west", "east" },
        Enumerable.Range(0, 6 * height).Select(i => (ushort)(i % 6 < 3 ? 1 : 2)).ToArray());

    private static List<PoliticalMapTriangle> Strokes(PoliticalMapGrid grid, params PoliticalMapSettlementPaint[] paints)
    {
        string[] territories = grid.SettlementIds.ToArray();
        var mesher = new PoliticalMapMesher(grid, territories);
        var chunks = territories.SelectMany(t => mesher.Build(t, paints)).ToList();
        Assert.All(chunks, c => Assert.InRange(c.TriangleCount, 1, PoliticalMapMesher.MaxTrianglesPerChunk));
        var strokes = chunks.Where(c => c.IsFrontier).SelectMany(c => c.Triangles).ToList();
        Assert.All(strokes, t => Assert.Equal(0xFFE8B92Cu, t.Color));
        return strokes;
    }

    [Fact]
    public void PeerRegionsHaveOneBroadOpaqueGoldSolidBoundaryEvenWhenTheirFillColoursMatch()
    {
        var strokes = Strokes(Grid(), Paint("duke_a"), Paint("duke_b"));
        Assert.NotEmpty(strokes);
        for (float y = 0.1f; y < 12; y += 0.2f) Assert.True(Covers(strokes, 3, y));
        Assert.True(Covers(strokes, 2.6f, 2));
        Assert.True(Covers(strokes, 3.4f, 2));
        Assert.False(Covers(strokes, 2.5f, 2));
        Assert.InRange(strokes.Sum(Area), 12 * 0.899, 12 * 0.901); // Drawn once, not once per side.
        Assert.Equal(24, strokes.Count); // Wider ribbons still use two triangles per straight edge.
    }

    [Theory]
    [InlineData("duke", "earl")]
    [InlineData("earl_a", "earl_b")]
    public void ALocalRulerAndVassalOrTwoOfItsVassalsShareADashedBoundary(string west, string east)
    {
        var strokes = Strokes(Grid(), Paint(west, "duke"), Paint(east, "duke"));
        Assert.True(Covers(strokes, 3, 1.2f));
        Assert.False(Covers(strokes, 3, 3.1f));
        Assert.True(Covers(strokes, 3, 5.2f));
        Assert.False(Covers(strokes, 3, 7.1f));
        Assert.True(Covers(strokes, 2.75f, 1.2f));
        Assert.False(Covers(strokes, 2.65f, 1.2f));
        Assert.InRange(strokes.Sum(Area), 7.2 * 0.599, 7.2 * 0.601);
    }

    [Fact]
    public void DifferentParentDomainsRemainSolidWhenZoomedIn()
    {
        var strokes = Strokes(Grid(), Paint("earl_a", "duke_a"), Paint("earl_b", "duke_b"));
        Assert.True(Covers(strokes, 3, 3.1f));
        Assert.True(Covers(strokes, 3, 7.1f));
    }

    [Fact]
    public void DashPhaseContinuesAcrossSettlementTerritoryAndTerrainBlockSeams()
    {
        var grid = new PoliticalMapGrid(6, 14, 1, new[] { "west", "east_south", "east_north" },
            Enumerable.Range(0, 6 * 14).Select(i => (ushort)(i % 6 < 3 ? 1 : i / 6 < 7 ? 2 : 3)).ToArray());
        var strokes = Strokes(grid, Paint("duke", "duke"), Paint("earl", "duke"), Paint("earl", "duke"));
        Assert.False(Covers(strokes, 3, 7.1f)); // New source settlement at row 7 is still in the gap.
        Assert.True(Covers(strokes, 3, 8.1f));
        Assert.True(Covers(strokes, 3, 9.9f));
        Assert.False(Covers(strokes, 3, 10.9f));
        Assert.False(Covers(strokes, 4.5f, 7)); // No frontier between two settlements of one region.
    }

    [Fact]
    public void CoastsWaterUnfilledLandAndSameRegionSeamsHaveNoStrokes()
    {
        Assert.Empty(Strokes(Grid(), Paint("duke"), Paint("duke")));
        Assert.Empty(Strokes(Grid(), Paint("duke"), PoliticalMapSettlementPaint.Blank));
        var coast = new PoliticalMapGrid(3, 3, 1, new[] { "island" }, new ushort[] { 0, 0, 0, 0, 1, 0, 0, 0, 0 });
        Assert.Empty(Strokes(coast, Paint("duke")));
    }

    [Fact]
    public void KingdomBoundariesUseTheFactionIdentityAndAreSolid()
    {
        var strokes = Strokes(Grid(), Paint("unused") with { RegionKey = null, RealmKey = "york" },
            Paint("unused") with { RegionKey = null, RealmKey = "lancaster" });
        Assert.True(Covers(strokes, 3, 3.1f));
    }

    [Fact]
    public void ChangingOnlyHierarchyMetadataRefreshesCachedStrokeClassification()
    {
        var grid = Grid();
        var mesher = new PoliticalMapMesher(grid, grid.SettlementIds);
        var paints = new[] { Paint("duke", "duke"), Paint("earl", "duke") };
        IEnumerable<PoliticalMapTriangle> Current() => grid.SettlementIds.SelectMany(t => mesher.Build(t, paints))
            .Where(c => c.IsFrontier).SelectMany(c => c.Triangles);
        Assert.False(Covers(Current(), 3, 3.1f));
        paints[1] = paints[1] with { ParentRegionKey = "other" };
        Assert.True(Covers(Current(), 3, 3.1f));
    }

    [Fact]
    public void BroadStrokesFollowTheSmoothedSharedBoundaryAcrossTerrainBlocks()
    {
        const int width = 24, height = 32;
        var grid = new PoliticalMapGrid(width, height, 1, new[] { "west", "east" },
            Enumerable.Range(0, width * height).Select(i => (ushort)(i % width < 11 + i / width % 2 ? 1 : 2)).ToArray());
        var strokes = Strokes(grid, Paint("duke_a"), Paint("duke_b"));

        // Raw alternating edges at 11 and 12 would leave this centre outside a 0.9-wide ribbon.
        for (float y = 8.1f; y < 24; y += 0.2f) Assert.True(Covers(strokes, 11.5f, y));
        Assert.All(strokes, t =>
        {
            Assert.True(Area(t) > 0);
            float[] x = { t.A.Column, t.B.Column, t.C.Column }, y = { t.A.Row, t.B.Row, t.C.Row };
            Assert.True(x.Max() <= (Math.Floor(x.Min() / 8) + 1) * 8 + 0.00001);
            Assert.True(y.Max() <= (Math.Floor(y.Min() / 8) + 1) * 8 + 0.00001);
        });
    }

    [Fact]
    public void BroadFrontiersAtShortSharpTurnsKeepPositiveWinding()
    {
        const int side = 24;
        var random = new Random(1471);
        var grid = new PoliticalMapGrid(side, side, 1, new[] { "west", "east" },
            Enumerable.Range(0, side * side).Select(_ => (ushort)random.Next(1, 3)).ToArray());

        var strokes = Strokes(grid, Paint("duke_a"), Paint("duke_b"));

        Assert.NotEmpty(strokes);
        Assert.All(strokes, triangle => Assert.True(Area(triangle) > 0));
    }

    [Fact]
    public void LongGoldFrontiersKeepThe512TriangleChunkCap()
    {
        var strokes = Strokes(Grid(600), Paint("duke_a"), Paint("duke_b"));

        Assert.Equal(1200, strokes.Count);
        Assert.InRange(strokes.Sum(Area), 600 * 0.899, 600 * 0.901);
    }

    private static double Area(PoliticalMapTriangle t) =>
        ((t.B.Column - t.A.Column) * (t.C.Row - t.A.Row) - (t.B.Row - t.A.Row) * (t.C.Column - t.A.Column)) * 0.5;

    private static bool Covers(IEnumerable<PoliticalMapTriangle> triangles, float x, float y)
    {
        return triangles.Any(t => Cross(t.A, t.B) >= -0.00001f && Cross(t.B, t.C) >= -0.00001f && Cross(t.C, t.A) >= -0.00001f);
        float Cross(PoliticalMapPoint a, PoliticalMapPoint b) => (b.Column - a.Column) * (y - a.Row) - (b.Row - a.Row) * (x - a.Column);
    }
}
