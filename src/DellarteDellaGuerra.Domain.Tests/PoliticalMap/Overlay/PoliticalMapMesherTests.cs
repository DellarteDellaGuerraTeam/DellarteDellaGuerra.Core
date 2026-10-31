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

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void SharedCornerGeometryTilesWithoutChangingOwnership(int minority)
        {
            var cells = new ushort[] { 2, 2, 2, 2 };
            cells[minority] = 1;
            var original = cells.ToArray();
            var grid = new PoliticalMapGrid(2, 2, 1f, new[] { "west", "east" }, cells);
            PoliticalMapMesher mesher = Mesher(grid);
            PoliticalMapSettlementPaint[] paints = Paints("r1", "r2");
            List<PoliticalMapTriangle> red = Geometry(mesher.Build("county_west", paints));
            List<PoliticalMapTriangle> blue = Geometry(mesher.Build("county_east", paints));

            Assert.InRange(red.Sum(Area), 0.5, 0.999);
            Assert.Equal(4, red.Concat(blue).Sum(Area), 5);
            Assert.Equal(original, grid.Cells);
            Assert.All(red, triangle => Assert.Equal(Red, triangle.Color));
            Assert.All(blue, triangle => Assert.Equal(Blue, triangle.Color));
            AssertTiling(red.Concat(blue).ToList(), grid);
        }

        [Fact]
        public void MatchingPaintsAcrossSettlementAndTerritorySeamsShareTheSameSmoothedFrontier()
        {
            var grid = new PoliticalMapGrid(2, 2, 1f, new[] { "a", "b", "c", "d" }, new ushort[] { 1, 2, 3, 4 });
            var territories = new[] { "a", "b", "c", "d" };
            var mesher = new PoliticalMapMesher(grid, territories);
            var paints = territories.Select((t, i) => new PoliticalMapSettlementPaint(i == 0 ? Red : Blue, 0, t, t, t)).ToArray();
            var geometry = territories.SelectMany(t => Geometry(mesher.Build(t, paints))).ToList();

            Assert.Equal(4, geometry.Sum(Area), 5);
            AssertTiling(geometry, grid);
            AssertCenters(geometry, grid, Red, Blue, Blue, Blue);
        }

        [Fact]
        public void MutablePaintChangesRecomputeTheSharedNetworkAndRestoringThemIsDeterministic()
        {
            var grid = new PoliticalMapGrid(2, 2, 1f, new[] { "west", "east" }, new ushort[] { 1, 2, 2, 2 });
            PoliticalMapMesher mesher = Mesher(grid);
            PoliticalMapSettlementPaint[] paints = Paints("r1", "r2");
            List<PoliticalMapTriangle> original = Geometry(mesher.Build("county_west", paints));
            Assert.InRange(original.Sum(Area), 0.5, 0.999);

            paints[1] = paints[1] with { Fill = Red };
            Assert.Empty(mesher.Build("county_west", paints).SelectMany(c => c.Triangles));
            Assert.Equal(1, Geometry(mesher.Build("county_west", paints)).Sum(Area));
            paints[1] = paints[1] with { Fill = Blue };
            Assert.Equal(original, Geometry(mesher.Build("county_west", paints)));
        }

        [Fact]
        public void CheckerboardsAndThreeWayJunctionsKeepTheirCommonAnchor()
        {
            foreach (ushort[] cells in new[] { new ushort[] { 1, 2, 2, 1 }, new ushort[] { 1, 2, 2, 3 } })
            {
                var grid = new PoliticalMapGrid(2, 2, 1f, new[] { "a", "b", "c" }, cells);
                var mesher = new PoliticalMapMesher(grid, new[] { "a", "b", "c" });
                PoliticalMapSettlementPaint[] paints = { new(Red, 0, "a", null, null), new(Blue, 0, "b", null, null), new(0x5A00FF00, 0, "c", null, null) };
                Assert.All(mesher.Territories.SelectMany(t => mesher.Build(t, paints)), chunk => Assert.Empty(chunk.Triangles));
            }
        }

        [Fact]
        public void AllLandWaterCornerPatternsKeepTheirOriginalCellCentersOnTheCorrectSide()
        {
            for (int mask = 1; mask < 16; mask++)
            {
                ushort[] cells = Enumerable.Range(0, 4).Select(i => (ushort)((mask & (1 << i)) == 0 ? 0 : 1)).ToArray();
                var grid = new PoliticalMapGrid(2, 2, 1f, new[] { "only" }, cells);
                var mesher = new PoliticalMapMesher(grid, new[] { "only" });
                PoliticalMapSettlementPaint[] paints = { new(Red, 0, "only", null, null) };
                List<PoliticalMapTriangle> geometry = Geometry(mesher.Build("only", paints));

                Assert.All(geometry, t => Assert.True(Area(t) > 0));
                AssertCenters(geometry, grid, Red);
            }
        }

        [Fact]
        public void AOneCellZigzagRiverKeepsAnOpenWaterChannelWhileBothBanksAreSmoothed()
        {
            const int side = 24;
            ushort[] cells = Enumerable.Range(0, side * side).Select(i =>
            {
                int river = 5 + i / side / 3;
                return (ushort)(i % side < river ? 1 : i % side == river ? 0 : 2);
            }).ToArray();
            var grid = new PoliticalMapGrid(side, side, 1f, new[] { "west", "east" }, cells);
            PoliticalMapMesher mesher = Mesher(grid);
            var geometry = mesher.Territories.SelectMany(t => Geometry(mesher.Build(t, Paints("r1", "r2")))).ToList();

            AssertCenters(geometry, grid, Red, Blue);
            Assert.Contains(geometry.SelectMany(Points), p => p.Column != (int)p.Column && p.Row != (int)p.Row);
            Assert.All(geometry, t => Assert.True(Area(t) > 0));
        }

        [Fact]
        public void DenseFrontiersTileAcrossBlockLinesAndStayWithinTheTriangleBudget()
        {
            const int side = 32;
            var random = new Random(71);
            ushort[] cells = Enumerable.Range(0, side * side).Select(_ => (ushort)random.Next(1, 4)).ToArray();
            var grid = new PoliticalMapGrid(side, side, 1f, new[] { "a", "b", "c" }, cells);
            var mesher = new PoliticalMapMesher(grid, new[] { "a", "b", "c" });
            uint[] colors = { Red, Blue, 0x5A00FF00 };
            PoliticalMapSettlementPaint[] paints = colors.Select(c => new PoliticalMapSettlementPaint(c, 0, null, null, null)).ToArray();
            var chunks = mesher.Territories.SelectMany(t => mesher.Build(t, paints)).ToList();
            List<PoliticalMapTriangle> geometry = Geometry(chunks);

            Assert.Equal(side * side, geometry.Sum(Area), 4);
            Assert.All(chunks, chunk => Assert.InRange(chunk.TriangleCount, 1, PoliticalMapMesher.MaxTrianglesPerChunk));
            AssertSheetBlocks(geometry);
            AssertTiling(geometry, grid);
            AssertCenters(geometry, grid, colors);
        }

        [Fact]
        public void RepeatedZigzagsAreSmoothedAcrossAllTerrainBlocksRatherThanPinnedAtTheirSeams()
        {
            int[] frontier = Enumerable.Range(0, 32).Select(row => 11 + row % 2).ToArray();
            PoliticalMapGrid grid = FrontierGrid(24, frontier);
            PoliticalMapMesher mesher = Mesher(grid);
            var geometry = mesher.Territories.SelectMany(t => Geometry(mesher.Build(t, Paints("a", "b")))).ToList();

            double[] boundary = Enumerable.Range(16, 96).Select(i => BoundaryAt(geometry, i / 4.0)).ToArray();
            Assert.True(boundary.Max() - boundary.Min() < 0.15, $"Remaining zigzag: {boundary.Max() - boundary.Min()}");
            Assert.Equal(grid.Cells.Length, geometry.Sum(Area), 4);
            AssertSheetBlocks(geometry);
            AssertTiling(geometry, grid);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void LongStaircasesHaveLowerBendingEnergyAndNoExtraKinkAtTerrainBlockSeams(bool transpose)
        {
            int[] frontier = Enumerable.Range(0, 32).Select(row => 4 + row / 2).ToArray();
            PoliticalMapGrid grid = FrontierGrid(32, frontier);
            if (transpose)
            {
                ushort[] original = grid.Cells.ToArray();
                for (int row = 0; row < 32; row++)
                    for (int column = 0; column < 32; column++) grid.Cells[row * 32 + column] = original[column * 32 + row];
            }
            PoliticalMapMesher mesher = Mesher(grid);
            var geometry = mesher.Territories.SelectMany(t => Geometry(mesher.Build(t, Paints("a", "b")))).ToList();
            List<PoliticalMapTriangle> measured = transpose ? geometry.Select(t => new PoliticalMapTriangle(
                new(t.A.Row, t.A.Column), new(t.C.Row, t.C.Column), new(t.B.Row, t.B.Column), t.Color)).ToList() : geometry;

            double oldEnergy = 0, newEnergy = 0;
            for (double y = 3.125; y < 29; y += 0.25)
            {
                oldEnergy += Math.Abs(frontier[(int)(y - 0.25)] - 2 * frontier[(int)y] + frontier[(int)(y + 0.25)]);
                newEnergy += Math.Abs(BoundaryAt(measured, y - 0.25) - 2 * BoundaryAt(measured, y) + BoundaryAt(measured, y + 0.25));
            }
            Assert.True(newEnergy < oldEnergy * 0.25, $"Bending energy {oldEnergy} -> {newEnergy}");
            foreach (int row in new[] { 8, 16, 24 })
                Assert.InRange(Math.Abs(BoundaryAt(measured, row - 0.001) - BoundaryAt(measured, row + 0.001)), 0, 0.01);
            Assert.Equal(1024, geometry.Sum(Area), 4);
            AssertSheetBlocks(geometry);
            AssertTiling(geometry, grid);
        }

        [Fact]
        public void ClosedLandAndWaterEnclavesKeepTheirCentersAndCannotDisappear()
        {
            foreach (ushort island in new ushort[] { 0, 1 })
            {
                PoliticalMapGrid grid = FrontierGrid(16, Enumerable.Repeat(3, 16).ToArray());
                grid.Cells[9 * 16 + 10] = island;
                PoliticalMapMesher mesher = Mesher(grid);
                var geometry = mesher.Territories.SelectMany(t => Geometry(mesher.Build(t, Paints("a", "b")))).ToList();
                AssertCenters(geometry, grid, Red, Blue);
                Assert.All(geometry, t => Assert.True(Area(t) > 0));
            }
        }

        [Theory]
        [InlineData(0, false)]
        [InlineData(1, false)]
        [InlineData(2, false)]
        [InlineData(3, false)]
        [InlineData(0, true)]
        [InlineData(1, true)]
        [InlineData(2, true)]
        [InlineData(3, true)]
        public void RotationsAndReflectionsKeepCellCentersColouredAndTheWholeMapTiled(int rotation, bool reflect)
        {
            const int side = 16;
            PoliticalMapGrid source = FrontierGrid(side, Enumerable.Range(0, side).Select(row => row + 1).ToArray());
            var cells = new ushort[side * side];
            for (int row = 0; row < side; row++)
                for (int column = 0; column < side; column++)
                {
                    int x = reflect ? side - 1 - column : column, y = row;
                    for (int i = 0; i < rotation; i++) (x, y) = (side - 1 - y, x);
                    cells[y * side + x] = source.At(column, row);
                }
            var grid = new PoliticalMapGrid(side, side, 1f, source.SettlementIds, cells);
            PoliticalMapMesher mesher = Mesher(grid);
            var geometry = mesher.Territories.SelectMany(t => Geometry(mesher.Build(t, Paints("a", "b")))).ToList();
            Assert.Equal(side * side, geometry.Sum(Area), 4);
            AssertSheetBlocks(geometry);
            AssertTiling(geometry, grid);
            AssertCenters(geometry, grid, Red, Blue);
        }

        private static IEnumerable<PoliticalMapPoint> Points(PoliticalMapTriangle t) => new[] { t.A, t.B, t.C };

        [Fact]
        public void RebuildingOnlyThePaintChangesSmoothingHaloMatchesAFreshFullMap()
        {
            const int width = 96, height = 24;
            var territories = Enumerable.Range(0, 12).Select(i => $"county_{i}").ToArray();
            ushort[] cells = Enumerable.Range(0, width * height)
                .Select(i => (ushort)(Math.Min(11, (i % width + i / width % 2) / 8) + 1)).ToArray();
            var grid = new PoliticalMapGrid(width, height, 1f, territories, cells);
            var mesher = new PoliticalMapMesher(grid, territories);
            PoliticalMapSettlementPaint[] paints = Enumerable.Range(0, 12)
                .Select(i => new PoliticalMapSettlementPaint(0x5A000000u + (uint)(i + 1), 0, territories[i], null, null)).ToArray();
            var shown = territories.ToDictionary(t => t, t => Geometry(mesher.Build(t, paints)));
            foreach (uint changedPaint in new[] { paints[2].Fill, 0u, Red })
            {
                paints[1] = paints[1] with { Fill = changedPaint };
                var dirty = new HashSet<int> { 1 };
                foreach ((int a, int b) in PoliticalMapSettlementAdjacency.InSharedBlocks(grid, PoliticalMapMesher.MaxQuadSide, PoliticalMapMesher.SmoothingReach))
                {
                    if (a == 1) dirty.Add(b);
                    if (b == 1) dirty.Add(a);
                }
                Assert.True(dirty.Count < territories.Length);
                foreach (int i in dirty) shown[territories[i]] = Geometry(mesher.Build(territories[i], paints));
                var fresh = new PoliticalMapMesher(grid, territories);
                foreach (string territory in territories)
                    Assert.Equal(Geometry(fresh.Build(territory, paints)), shown[territory]);
            }
        }

        private static void AssertSheetBlocks(List<PoliticalMapTriangle> geometry) => Assert.All(geometry, t =>
        {
            Assert.True(Area(t) > 0);
            float[] x = Points(t).Select(p => p.Column).ToArray(), y = Points(t).Select(p => p.Row).ToArray();
            Assert.Equal((int)(x.Min() / 8), (int)((x.Max() - 0.00001f) / 8));
            Assert.Equal((int)(y.Min() / 8), (int)((y.Max() - 0.00001f) / 8));
        });

        private static void AssertCenters(List<PoliticalMapTriangle> geometry, PoliticalMapGrid grid, params uint[] colors)
        {
            for (int row = 0; row < grid.Height; row++)
            for (int column = 0; column < grid.Width; column++)
            {
                // A tiny non-diagonal offset avoids counting a point exactly on a mesh diagonal twice.
                var point = new PoliticalMapPoint(column + 0.5001f, row + 0.5f);
                var covering = geometry.Where(t => Cross(t.A, t.B, point) > 0 && Cross(t.B, t.C, point) > 0 && Cross(t.C, t.A, point) > 0).ToList();
                ushort cell = grid.At(column, row);
                if (cell == 0) Assert.Empty(covering);
                else Assert.Equal(colors[cell - 1], Assert.Single(covering).Color);
            }
        }

        private static double BoundaryAt(List<PoliticalMapTriangle> geometry, double y)
        {
            double rightmost = double.NegativeInfinity;
            foreach (PoliticalMapTriangle triangle in geometry.Where(t => t.Color == Red))
            {
                PoliticalMapPoint[] points = Points(triangle).ToArray();
                for (int j = 0; j < 3; j++)
                {
                    PoliticalMapPoint a = points[j], b = points[(j + 1) % 3];
                    if (y < Math.Min(a.Row, b.Row) || y > Math.Max(a.Row, b.Row) || a.Row == b.Row) continue;
                    rightmost = Math.Max(rightmost, a.Column + (y - a.Row) / (b.Row - a.Row) * (b.Column - a.Column));
                }
            }
            return rightmost;
        }

        private static PoliticalMapGrid FrontierGrid(int width, int[] frontier) => new(width, frontier.Length, 1f,
            new[] { "west", "east" }, Enumerable.Range(0, width * frontier.Length)
                .Select(i => (ushort)(i % width < frontier[i / width] ? 1 : 2)).ToArray());

        private static List<PoliticalMapTriangle> Geometry(IEnumerable<PoliticalMapMeshChunk> chunks) => chunks.SelectMany(chunk =>
            chunk.Triangles.Concat(chunk.Quads.SelectMany(quad =>
            {
                var a = new PoliticalMapPoint(quad.Column, quad.Row);
                var b = new PoliticalMapPoint(quad.Column + quad.Width, quad.Row);
                var c = new PoliticalMapPoint(quad.Column + quad.Width, quad.Row + quad.Height);
                var d = new PoliticalMapPoint(quad.Column, quad.Row + quad.Height);
                return new[] { new PoliticalMapTriangle(a, b, c, quad.Color), new PoliticalMapTriangle(a, c, d, quad.Color) };
            }))).ToList();

        private static double Area(PoliticalMapTriangle t) => Cross(t.A, t.B, t.C) / 2;

        private static float Cross(PoliticalMapPoint a, PoliticalMapPoint b, PoliticalMapPoint p) =>
            (b.Column - a.Column) * (p.Row - a.Row) - (b.Row - a.Row) * (p.Column - a.Column);

        private static void AssertTiling(List<PoliticalMapTriangle> geometry, PoliticalMapGrid grid)
        {
            for (int row = 0; row < grid.Height; row++)
            {
                for (int column = 0; column < grid.Width; column++)
                {
                    foreach (float dx in new[] { 0.173f, 0.413f, 0.733f })
                    {
                        var point = new PoliticalMapPoint(column + dx, row + 0.237f);
                        int covering = geometry.Count(t => Cross(t.A, t.B, point) > 0 && Cross(t.B, t.C, point) > 0 && Cross(t.C, t.A, point) > 0);
                        Assert.Equal(grid.Cells[row * grid.Width + column] == PoliticalMapGrid.None ? 0 : 1, covering);
                    }
                }
            }
        }
    }
}
