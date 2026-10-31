using System.Collections.Generic;
using DellarteDellaGuerra.Domain.PoliticalMap.Grid;

namespace DellarteDellaGuerra.Domain.PoliticalMap.Overlay
{
    /// <summary>Which grid settlements touch: two settlements are neighbours when their cells share a side.</summary>
    public static class PoliticalMapSettlementAdjacency
    {
        /// <summary>Every neighbouring pair once, as 0-based settlement indices with the smaller first.</summary>
        public static IReadOnlyList<(int A, int B)> Of(PoliticalMapGrid grid)
        {
            var pairs = new HashSet<(int, int)>();
            for (int index = 0; index < grid.Cells.Length; index++)
            {
                ushort cell = grid.Cells[index];
                if (cell == PoliticalMapGrid.None) continue;

                if (index % grid.Width > 0) Touch(grid.Cells[index - 1]);
                if (index >= grid.Width) Touch(grid.Cells[index - grid.Width]);

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
