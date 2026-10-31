using DellarteDellaGuerra.Domain.PoliticalMap.Grid;
using DellarteDellaGuerra.Domain.PoliticalMap.Overlay;

namespace DellarteDellaGuerra.Domain.Tests.PoliticalMap.Overlay
{
    public class PoliticalMapMesherTests
    {
        private const uint Red = 0x5AFF0000;
        private const uint Blue = 0x5A0000FF;

        // Two settlements side by side, each 8 cells wide and 4 tall.
        private static PoliticalMapGrid TwoHalves(int halfWidth = 8, int height = 4)
        {
            int width = halfWidth * 2;
            var cells = new ushort[width * height];
            for (int index = 0; index < cells.Length; index++) cells[index] = (ushort)(index % width < halfWidth ? 1 : 2);
            return new PoliticalMapGrid(width, height, 1f, new[] { "west", "east" }, cells);
        }

        private static PoliticalMapMesher Mesher(PoliticalMapGrid grid) => new(grid, new[] { "county_west", "county_east" });

        private static IEnumerable<(int Column, int Row)> Cells(IEnumerable<PoliticalMapMeshChunk> chunks) =>
            chunks.SelectMany(chunk => chunk.Quads)
                .SelectMany(quad => Enumerable.Range(0, quad.Width)
                    .SelectMany(dx => Enumerable.Range(0, quad.Height).Select(dy => (quad.Column + dx, quad.Row + dy))));

        private static PoliticalMapSettlementPaint[] Paints(string? westRealm, string? eastRealm) => new[]
        {
            new PoliticalMapSettlementPaint(Red, 0, "county_west", "d1", westRealm),
            new PoliticalMapSettlementPaint(Blue, 0, "county_east", "d2", eastRealm)
        };

        [Fact]
        public void TheFillCoversEveryCellOfTheTerritoryInItsColourWithNoBorderAlongAnyKeyChange()
        {
            IReadOnlyList<PoliticalMapMeshChunk> chunks = Mesher(TwoHalves()).Build("county_west", Paints("r1", "r2"));

            List<(int Column, int Row)> cells = Cells(chunks).ToList();
            Assert.Equal(32, cells.Count);
            Assert.Equal(32, cells.Distinct().Count());
            Assert.All(cells, cell => Assert.InRange(cell.Column, 0, 7));
            Assert.All(chunks.SelectMany(chunk => chunk.Quads), quad => Assert.Equal(Red, quad.Color));
        }

        [Fact]
        public void AHatchStripesTheFillDiagonallyFourCellsWide()
        {
            PoliticalMapSettlementPaint[] paints =
            {
                new(Red, Blue, "county_west", null, null),
                new(Blue, 0, "county_east", null, null)
            };

            List<PoliticalMapQuad> quads = Mesher(TwoHalves()).Build("county_west", paints)
                .SelectMany(chunk => chunk.Quads).ToList();

            uint ColourAt(int column, int row) => quads.Single(quad =>
                column >= quad.Column && column < quad.Column + quad.Width && row >= quad.Row && row < quad.Row + quad.Height).Color;

            Assert.Equal(Red, ColourAt(0, 0));
            Assert.Equal(Red, ColourAt(3, 0));
            Assert.Equal(Blue, ColourAt(4, 0));
            Assert.Equal(Blue, ColourAt(3, 1));
            Assert.Equal(Red, ColourAt(7, 1));
        }

        [Fact]
        public void QuadsStayWithinEightCellsASideAndChunksWithin256Quads()
        {
            // 40 x 40 hatched cells: the diagonal stripes break into hundreds of short runs.
            var cells = new ushort[40 * 40];
            for (int index = 0; index < cells.Length; index++) cells[index] = 1;
            var grid = new PoliticalMapGrid(40, 40, 1f, new[] { "only" }, cells);
            var mesher = new PoliticalMapMesher(grid, new[] { "county_only" });
            PoliticalMapSettlementPaint[] paints = { new(Red, Blue, "county_only", null, null) };

            IReadOnlyList<PoliticalMapMeshChunk> chunks = mesher.Build("county_only", paints);

            Assert.All(chunks, chunk => Assert.InRange(chunk.Quads.Count, 1, PoliticalMapMesher.MaxQuadsPerChunk));
            Assert.All(chunks.SelectMany(chunk => chunk.Quads), quad =>
            {
                Assert.InRange(quad.Width, 1, PoliticalMapMesher.MaxQuadSide);
                Assert.InRange(quad.Height, 1, PoliticalMapMesher.MaxQuadSide);
            });
            Assert.Equal(1600, Cells(chunks).Count());
            Assert.True(chunks.Count > 1);
        }

        [Fact]
        public void QuadsNeverCrossABlockLineOfTheWholeGrid()
        {
            // The east settlement starts at column 3, off the block lines at 0, 8 and 16.
            var cells = new ushort[21 * 13];
            for (int index = 0; index < cells.Length; index++) cells[index] = (ushort)(index % 21 < 3 ? 1 : 2);
            var grid = new PoliticalMapGrid(21, 13, 1f, new[] { "west", "east" }, cells);

            List<PoliticalMapQuad> quads = Mesher(grid).Build("county_east", Paints("r", "r"))
                .SelectMany(chunk => chunk.Quads).ToList();

            Assert.All(quads, quad =>
            {
                Assert.Equal(quad.Column / PoliticalMapMesher.MaxQuadSide, (quad.Column + quad.Width - 1) / PoliticalMapMesher.MaxQuadSide);
                Assert.Equal(quad.Row / PoliticalMapMesher.MaxQuadSide, (quad.Row + quad.Height - 1) / PoliticalMapMesher.MaxQuadSide);
            });
            Assert.Equal(6, quads.Count);
        }

        [Fact]
        public void AnUnhatchedTerritoryMergesIntoFewQuads()
        {
            IReadOnlyList<PoliticalMapMeshChunk> chunks = Mesher(TwoHalves()).Build("county_west", Paints("r", "r"));

            Assert.Single(chunks.SelectMany(chunk => chunk.Quads));
        }

        [Fact]
        public void EveryTerritoryWithCellsIsMeshed()
        {
            PoliticalMapMesher mesher = Mesher(TwoHalves());

            Assert.Equal(new[] { "county_west", "county_east" }.OrderBy(t => t), mesher.Territories.OrderBy(t => t));
            Assert.Empty(mesher.Build("county_nowhere", Paints("r", "r")));
        }
    }
}
