using DellarteDellaGuerra.Domain.PoliticalMap.Grid;

namespace DellarteDellaGuerra.Domain.Tests.PoliticalMap
{
    public class PoliticalMapGridBuilderTests
    {
        [Fact]
        public void LandGoesToTheNearestSettlementAndWaterToNone()
        {
            PoliticalMapGrid grid = Build(
                new[]
                {
                    "..........",
                    "..........",
                    "..........",
                    "...LLLL...",
                    "...LLLL...",
                    "..........",
                    "..........",
                    "..........",
                    "..........",
                    "..........",
                },
                Seed("west", 3.2f, 5.2f),
                Seed("east", 6.2f, 5.2f));

            Assert.Equal(
                new[]
                {
                    "..........",
                    "..........",
                    "..........",
                    "...WWEE...",
                    "...WWEE...",
                    "..........",
                    "..........",
                    "..........",
                    "..........",
                    "..........",
                },
                Picture(grid, "west", "east"));
        }

        [Fact]
        public void ANarrowRiverDoesNotStopASettlementButStaysWater()
        {
            // The river is one cell wide: the town reaches over it to the far bank, the castle at
            // the far end of that bank is further away over land, and the river itself is unpainted.
            PoliticalMapGrid grid = Build(
                new[]
                {
                    "..............",
                    "..............",
                    "..............",
                    "...LLL.LLLLL..",
                    "...LLL.LLLLL..",
                    "...LLL.LLLLL..",
                    "..............",
                    "..............",
                    "..............",
                    "..............",
                    "..............",
                    "..............",
                    "..............",
                    "..............",
                },
                Seed("town", 4.2f, 9.2f),
                Seed("castle", 11.2f, 9.2f));

            Assert.Equal(
                new[]
                {
                    "..............",
                    "..............",
                    "..............",
                    "...TTT.TCCCC..",
                    "...TTT.TCCCC..",
                    "...TTT.TCCCC..",
                    "..............",
                    "..............",
                    "..............",
                    "..............",
                    "..............",
                    "..............",
                    "..............",
                    "..............",
                },
                Picture(grid, "town", "castle"));
        }

        [Fact]
        public void ASettlementDoesNotReachAcrossTheSeaEvenWhenItIsNearer()
        {
            // As the crow flies, the near shore of the east island is closest to the west town.
            PoliticalMapGrid grid = Build(
                new[]
                {
                    "....................",
                    "....................",
                    "....................",
                    "..LL.....LLLLLLLLL..",
                    "..LL.....LLLLLLLLL..",
                    "....................",
                    "....................",
                    "....................",
                    "....................",
                    "....................",
                    "....................",
                    "....................",
                    "....................",
                    "....................",
                    "....................",
                    "....................",
                    "....................",
                    "....................",
                    "....................",
                    "....................",
                },
                Seed("west", 3.2f, 15.2f),
                Seed("east", 17.2f, 15.2f));

            Assert.Equal("..WW.....EEEEEEEEE..", Picture(grid, "west", "east")[3]);
        }

        [Fact]
        public void LandNoSettlementCanReachBelongsToNoOne()
        {
            PoliticalMapGrid grid = Build(
                new[]
                {
                    "..............",
                    "..............",
                    "..............",
                    "...LL.....LL..",
                    "...LL.....LL..",
                    "..............",
                    "..............",
                    "..............",
                    "..............",
                    "..............",
                    "..............",
                    "..............",
                    "..............",
                    "..............",
                },
                Seed("town", 3.2f, 9.2f));

            Assert.Equal("...TT.........", Picture(grid, "town")[3]);
        }

        [Fact]
        public void ASettlementStandingInWaterClaimsTheNearestLand()
        {
            PoliticalMapGrid grid = Build(
                new[]
                {
                    "..........",
                    "..........",
                    "..........",
                    "...LLLL...",
                    "...LLLL...",
                    "..........",
                    "..........",
                    "..........",
                    "..........",
                    "..........",
                },
                Seed("harbour", 8.2f, 6.2f),
                Seed("inland", 3.2f, 6.2f));

            Assert.Equal("...IIHH...", Picture(grid, "harbour", "inland")[3]);
            Assert.Null(grid.SettlementAt(8.2f, 6.2f));
        }

        [Fact]
        public void TwoSettlementsOnOneSpotLeaveItToTheFirstAndKeepBothListed()
        {
            PoliticalMapGrid grid = Build(
                new[]
                {
                    "........",
                    "........",
                    "........",
                    "...LL...",
                    "........",
                    "........",
                    "........",
                    "........",
                },
                Seed("first", 3.2f, 4.2f),
                Seed("second", 3.2f, 4.2f));

            Assert.Equal(new[] { "first", "second" }, grid.SettlementIds);
            Assert.Equal("first", grid.SettlementAt(4.2f, 4.2f));
            Assert.DoesNotContain((ushort)2, grid.Cells);
        }

        [Fact]
        public void APositionOffTheMapHasNoSettlement()
        {
            PoliticalMapGrid grid = Build(new[] { "...", "...", "..." }, Seed("town", 1.2f, 1.2f));

            Assert.Null(grid.SettlementAt(-0.5f, 1f));
            Assert.Null(grid.SettlementAt(1f, 3.5f));
        }

        [Fact]
        public void TheHeightIsInterpolatedBetweenSamplesOverTheRange()
        {
            var terrain = new PoliticalMapHeightField(2, new ushort[] { 0, 65535, 0, 65535 }, 2f, 12f, 10f);

            Assert.Equal(2f, terrain.HeightAt(0, 0));
            Assert.Equal(7f, terrain.HeightAt(5, 3));
            Assert.Equal(12f, terrain.HeightAt(10, 10));
        }

        private static PoliticalMapSeed Seed(string id, float x, float y) => new PoliticalMapSeed(id, x, y);

        /// <summary>
        /// Builds over a map drawn top row first, one 1 m cell per character: 'L' is land standing
        /// above the sea, anything else is water.
        /// </summary>
        private static PoliticalMapGrid Build(string[] picture, params PoliticalMapSeed[] seeds)
        {
            int cells = picture.Length;
            // Two samples per cell, so every cell centre lands exactly on a sample.
            int side = 2 * cells + 1;
            var samples = new ushort[side * side];
            for (int row = 0; row < side; row++)
            {
                for (int column = 0; column < side; column++)
                {
                    int cellRow = System.Math.Min(row / 2, cells - 1);
                    int cellColumn = System.Math.Min(column / 2, cells - 1);
                    bool land = picture[cells - 1 - cellRow][cellColumn] == 'L';
                    samples[row * side + column] = land ? ushort.MaxValue : (ushort)0;
                }
            }

            return PoliticalMapGridBuilder.Build(new PoliticalMapHeightField(side, samples, 0f, 20f, cells), seeds);
        }

        /// <summary>The grid drawn back top row first: each owner by the first letter of its id, upper-cased.</summary>
        private static string[] Picture(PoliticalMapGrid grid, params string[] ids)
        {
            var rows = new string[grid.Height];
            for (int row = 0; row < grid.Height; row++)
            {
                var line = new char[grid.Width];
                for (int column = 0; column < grid.Width; column++)
                {
                    ushort cell = grid.At(column, row);
                    line[column] = cell == PoliticalMapGrid.None ? '.' : char.ToUpperInvariant(grid.SettlementIds[cell - 1][0]);
                }

                rows[grid.Height - 1 - row] = new string(line);
            }

            return rows;
        }
    }
}
