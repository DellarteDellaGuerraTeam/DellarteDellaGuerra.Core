using DellarteDellaGuerra.Domain.PoliticalMap.Grid;
using DellarteDellaGuerra.Domain.PoliticalMap.Overlay;

namespace DellarteDellaGuerra.Domain.Tests.PoliticalMap.Overlay
{
    public class PoliticalMapSheetTests
    {
        private const int Cells = 20;

        // Rough random terrain: 41 samples over 20 m, so two samples a cell.
        private static PoliticalMapHeightField Hills(int seed = 7)
        {
            var random = new Random(seed);
            var samples = new ushort[41 * 41];
            for (int index = 0; index < samples.Length; index++) samples[index] = (ushort)random.Next(65536);
            return new PoliticalMapHeightField(41, samples, -5f, 30f, Cells);
        }

        private static PoliticalMapGrid Grid() => new(Cells, Cells, 1f, new[] { "only" }, new ushort[Cells * Cells]);

        [Fact]
        public void EveryCellCornerOfABlockIsAboveAllOfTheBlocksTerrain()
        {
            PoliticalMapHeightField terrain = Hills();
            var sheet = new PoliticalMapSheet(terrain, Grid());

            for (int blockRow = 0; blockRow < Cells; blockRow += PoliticalMapMesher.MaxQuadSide)
            {
                for (int blockColumn = 0; blockColumn < Cells; blockColumn += PoliticalMapMesher.MaxQuadSide)
                {
                    int right = Math.Min(blockColumn + PoliticalMapMesher.MaxQuadSide, Cells);
                    int top = Math.Min(blockRow + PoliticalMapMesher.MaxQuadSide, Cells);

                    float lowestCorner = float.MaxValue;
                    for (int row = blockRow; row <= top; row++)
                    {
                        for (int column = blockColumn; column <= right; column++) lowestCorner = Math.Min(lowestCorner, sheet.HeightAt(column, row));
                    }

                    float highestTerrain = float.MinValue;
                    for (double y = blockRow; y <= top; y += 0.1)
                    {
                        for (double x = blockColumn; x <= right; x += 0.1) highestTerrain = Math.Max(highestTerrain, terrain.HeightAt(x, y));
                    }

                    Assert.True(lowestCorner >= highestTerrain, $"Block at {blockColumn},{blockRow}: sheet {lowestCorner} under terrain {highestTerrain}.");
                }
            }
        }

        [Fact]
        public void AlongAGridLineInsideABlockTheSheetIsStraight()
        {
            var sheet = new PoliticalMapSheet(Hills(), Grid());

            for (int row = 0; row <= Cells; row++)
            {
                for (int column = 0; column <= Cells; column++)
                {
                    int left = column / PoliticalMapMesher.MaxQuadSide * PoliticalMapMesher.MaxQuadSide;
                    int right = left + PoliticalMapMesher.MaxQuadSide;
                    float along = (column - left) / (float)PoliticalMapMesher.MaxQuadSide;
                    float straight = sheet.HeightAt(left, row) * (1 - along) + sheet.HeightAt(right, row) * along;

                    Assert.Equal(straight, sheet.HeightAt(column, row), 0.0001);
                }
            }
        }

        [Fact]
        public void OverFlatTerrainTheSheetLiesOnIt()
        {
            var samples = new ushort[41 * 41];
            for (int index = 0; index < samples.Length; index++) samples[index] = 32768;
            var terrain = new PoliticalMapHeightField(41, samples, 0f, 10f, Cells);
            var sheet = new PoliticalMapSheet(terrain, Grid());

            Assert.Equal(terrain.HeightAt(3, 17), sheet.HeightAt(3, 17), 4);
            Assert.Equal(terrain.HeightAt(20, 20), sheet.HeightAt(20, 20), 4);
        }
    }
}
