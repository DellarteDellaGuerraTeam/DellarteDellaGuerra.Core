using DellarteDellaGuerra.Domain.PoliticalMap.Grid;
using DellarteDellaGuerra.Domain.PoliticalMap.Labels;

namespace DellarteDellaGuerra.Domain.Tests.PoliticalMap.Labels
{
    public class PoliticalMapRegionsTests
    {
        // Rows from y = 0 up; each character is a settlement index (1-based), '.' is water.
        private static PoliticalMapGrid Grid(params string[] rows)
        {
            int width = rows[0].Length;
            var cells = new ushort[width * rows.Length];
            for (int row = 0; row < rows.Length; row++)
            {
                for (int column = 0; column < width; column++)
                {
                    char c = rows[row][column];
                    cells[row * width + column] = c == '.' ? PoliticalMapGrid.None : (ushort)(c - '0');
                }
            }

            return new PoliticalMapGrid(width, rows.Length, 1f, new[] { "s1", "s2", "s3", "s4" }, cells);
        }

        [Fact]
        public void AlternativeAnchorsNeverCrossWaterToAnotherHoldingWithTheSameName()
        {
            var grid = Grid("111.111", "111.111", "111.111");
            var keys = new[] { "a" };
            var region = PoliticalMapRegions.Find(grid, keys, 1)[0];
            var anchors = PoliticalMapRegions.AlternativeAnchors(grid, keys, region);
            Assert.NotEmpty(anchors);
            Assert.All(anchors, anchor => Assert.True(anchor.X >= 0 && anchor.X < 3));
        }

        [Fact]
        public void SettlementsSharingAKeyMakeOneRegion()
        {
            PoliticalMapGrid grid = Grid(
                "1122",
                "1122");

            PoliticalMapRegion region = Assert.Single(PoliticalMapRegions.Find(grid, new[] { "a", "a" }, 1));

            Assert.Equal("a", region.Key);
            Assert.Equal(8, region.CellCount);
        }

        [Fact]
        public void WaterSplitsOneKeyIntoTwoRegions()
        {
            PoliticalMapGrid grid = Grid(
                "111.22",
                "111.22");

            IReadOnlyList<PoliticalMapRegion> regions = PoliticalMapRegions.Find(grid, new[] { "a", "a" }, 1);

            Assert.Equal(new[] { 6, 4 }, regions.Select(region => region.CellCount));
            Assert.All(regions, region => Assert.Equal("a", region.Key));
        }

        [Fact]
        public void DiagonalCellsAreNotConnected()
        {
            PoliticalMapGrid grid = Grid(
                "1.",
                ".1");

            Assert.Equal(2, PoliticalMapRegions.Find(grid, new[] { "a" }, 1).Count);
        }

        [Fact]
        public void DifferentKeysMakeDifferentRegions()
        {
            PoliticalMapGrid grid = Grid(
                "1112",
                "1112");

            IReadOnlyList<PoliticalMapRegion> regions = PoliticalMapRegions.Find(grid, new[] { "a", "b" }, 1);

            Assert.Equal(new[] { "a", "b" }, regions.Select(region => region.Key));
        }

        [Fact]
        public void SettlementsWithoutAKeyBelongToNoRegion()
        {
            PoliticalMapGrid grid = Grid(
                "1122",
                "1122");

            PoliticalMapRegion region = Assert.Single(PoliticalMapRegions.Find(grid, new string?[] { null, "b" }, 1));

            Assert.Equal("b", region.Key);
        }

        [Fact]
        public void FragmentsBelowTheThresholdAreLeftOut()
        {
            PoliticalMapGrid grid = Grid(
                "1111.1",
                "1111..");

            PoliticalMapRegion region = Assert.Single(PoliticalMapRegions.Find(grid, new[] { "a" }, 2));

            Assert.Equal(8, region.CellCount);
        }

        [Fact]
        public void ASquareIsLabelledAtItsCentre()
        {
            PoliticalMapGrid grid = Grid(
                "11111",
                "11111",
                "11111",
                "11111",
                "11111");

            PoliticalMapRegion region = Assert.Single(PoliticalMapRegions.Find(grid, new[] { "a" }, 1));

            Assert.Equal(2.5f, region.X);
            Assert.Equal(2.5f, region.Y);
            // Three cells from the outside: the circle reaches the middle of the edge cells.
            Assert.Equal(2.5f, region.Radius);
        }

        [Fact]
        public void AnLIsLabelledOnItsLandNotAtItsCentroid()
        {
            // An L three cells thick; its centroid falls in the water of the crook.
            PoliticalMapGrid grid = Grid(
                "111111111",
                "111111111",
                "111111111",
                "111......",
                "111......",
                "111......",
                "111......",
                "111......",
                "111......");

            PoliticalMapRegion region = Assert.Single(PoliticalMapRegions.Find(grid, new[] { "a" }, 1));

            Assert.Equal("s1", grid.SettlementAt(region.X, region.Y));
            Assert.Equal(1.5f, region.Radius);
        }

        [Fact]
        public void TheCrookOfAThickLWinsOverItsArms()
        {
            PoliticalMapGrid grid = Grid(
                "1111111111",
                "1111111111",
                "1111111111",
                "1111111111",
                "1111......",
                "1111......",
                "1111......",
                "1111......",
                "1111......",
                "1111......");

            PoliticalMapRegion region = Assert.Single(PoliticalMapRegions.Find(grid, new[] { "a" }, 1));

            // The corner square is where both arms meet: the only place a circle reaches past 2 cells.
            Assert.InRange(region.X, 0f, 4f);
            Assert.InRange(region.Y, 0f, 4f);
        }

        [Fact]
        public void ANeighbourCountsAsOutside()
        {
            PoliticalMapGrid grid = Grid(
                "11111",
                "11111",
                "11211",
                "11111",
                "11111");

            PoliticalMapRegion region = PoliticalMapRegions.Find(grid, new[] { "a", "b" }, 1)[0];

            // Without the hole the circle would be 2.5 cells; the enclave pinches it to under one.
            Assert.Equal("a", region.Key);
            Assert.True(region.Radius < 1f);
        }

        [Fact]
        public void RegionsComeLargestFirst()
        {
            PoliticalMapGrid grid = Grid(
                "1.222",
                "1.222");

            IReadOnlyList<PoliticalMapRegion> regions = PoliticalMapRegions.Find(grid, new[] { "a", "b" }, 1);

            Assert.Equal(new[] { "b", "a" }, regions.Select(region => region.Key));
        }

        [Fact]
        public void APolePositionIsInMetersByCellSize()
        {
            var grid = new PoliticalMapGrid(1, 1, 4f, new[] { "s1" }, new ushort[] { 1 });

            PoliticalMapRegion region = Assert.Single(PoliticalMapRegions.Find(grid, new[] { "a" }, 1));

            Assert.Equal(2f, region.X);
            Assert.Equal(2f, region.Y);
            Assert.Equal(2f, region.Radius);
        }
    }
}
