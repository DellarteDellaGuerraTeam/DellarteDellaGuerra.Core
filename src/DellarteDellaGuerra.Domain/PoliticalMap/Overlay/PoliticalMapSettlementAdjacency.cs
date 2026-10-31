using System.Collections.Generic;
using DellarteDellaGuerra.Domain.PoliticalMap.Grid;

namespace DellarteDellaGuerra.Domain.PoliticalMap.Overlay
{
    /// <summary>Which grid settlements touch: two settlements are neighbours when their cells share a side.</summary>
    public static class PoliticalMapSettlementAdjacency
    {
        /// <summary>Settlements sharing a block or its smoothing halo, for geometry refresh.</summary>
        public static IReadOnlyList<(int A, int B)> InSharedBlocks(PoliticalMapGrid grid, int blockSide, int halo = 0)
        {
            if (blockSide < 1) throw new System.ArgumentOutOfRangeException(nameof(blockSide));
            if (halo < 0) throw new System.ArgumentOutOfRangeException(nameof(halo));
            var pairs = new HashSet<(int A, int B)>();
            var settlements = new List<int>();
            for (int row = 0; row < grid.Height; row += blockSide)
            {
                for (int column = 0; column < grid.Width; column += blockSide)
                {
                    settlements.Clear();
                    for (int y = System.Math.Max(0, row - halo); y < System.Math.Min(row + blockSide + halo, grid.Height); y++)
                    for (int x = System.Math.Max(0, column - halo); x < System.Math.Min(column + blockSide + halo, grid.Width); x++)
                    {
                        int settlement = grid.At(x, y) - 1;
                        if (settlement >= 0 && !settlements.Contains(settlement)) settlements.Add(settlement);
                    }

                    for (int a = 0; a < settlements.Count; a++)
                    for (int b = a + 1; b < settlements.Count; b++)
                    {
                        int first = settlements[a], second = settlements[b];
                        pairs.Add(first < second ? (first, second) : (second, first));
                    }
                }
            }

            var sorted = new List<(int A, int B)>(pairs);
            sorted.Sort();
            return sorted;
        }

        /// <summary>Every neighbouring pair once, as 0-based settlement indices with the smaller first.</summary>
        public static IReadOnlyList<(int A, int B)> Of(PoliticalMapGrid grid, bool includeCorners = false)
        {
            var pairs = new HashSet<(int, int)>();
            for (int index = 0; index < grid.Cells.Length; index++)
            {
                ushort cell = grid.Cells[index];
                if (cell == PoliticalMapGrid.None) continue;

                if (index % grid.Width > 0) Touch(grid.Cells[index - 1]);
                if (index >= grid.Width) Touch(grid.Cells[index - grid.Width]);
                if (includeCorners && index >= grid.Width)
                {
                    if (index % grid.Width > 0) Touch(grid.Cells[index - grid.Width - 1]);
                    if (index % grid.Width < grid.Width - 1) Touch(grid.Cells[index - grid.Width + 1]);
                }

                void Touch(ushort other)
                {
                    if (other == PoliticalMapGrid.None || other == cell) return;
                    int a = cell - 1, b = other - 1;
                    pairs.Add(a < b ? (a, b) : (b, a));
                }
            }

            var sorted = new List<(int A, int B)>(pairs);
            sorted.Sort();
            return sorted;
        }
    }
}
