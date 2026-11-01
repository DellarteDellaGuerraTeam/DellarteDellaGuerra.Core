using System;
using System.Collections.Generic;
using DellarteDellaGuerra.Domain.PoliticalMap.Grid;

namespace DellarteDellaGuerra.Domain.PoliticalMap.Labels
{
    /// <summary>Places a coarse holder label on that holder's largest detailed region in the same component.</summary>
    public static class PoliticalMapOwnerLabelAnchors
    {
        public static IReadOnlyList<PoliticalMapRegion> Apply(
            PoliticalMapGrid grid, IReadOnlyList<string?> coarseKeys,
            IReadOnlyList<PoliticalMapRegion> coarse, IReadOnlyList<PoliticalMapRegion> detailed)
        {
            if (coarse.Count == 0 || detailed.Count == 0) return coarse;

            // Component membership matters: one holder can have several disconnected blocks of land.
            var component = new int[grid.Cells.Length];
            var stack = new int[grid.Cells.Length];
            for (int index = 0; index < coarse.Count; index++)
            {
                PoliticalMapRegion region = coarse[index];
                int start = CellOf(region);
                if (start < 0 || component[start] != 0) continue;
                int top = 0;
                Push(start);
                while (top > 0)
                {
                    int cell = stack[--top];
                    int column = cell % grid.Width;
                    int row = cell / grid.Width;
                    if (column > 0) Push(cell - 1);
                    if (column + 1 < grid.Width) Push(cell + 1);
                    if (row > 0) Push(cell - grid.Width);
                    if (row + 1 < grid.Height) Push(cell + grid.Width);
                }

                void Push(int cell)
                {
                    if (component[cell] != 0) return;
                    ushort settlement = grid.Cells[cell];
                    if (settlement == PoliticalMapGrid.None || settlement > coarseKeys.Count
                        || !StringComparer.Ordinal.Equals(coarseKeys[settlement - 1], region.Key)) return;
                    component[cell] = index + 1;
                    stack[top++] = cell;
                }
            }

            var anchors = new PoliticalMapRegion?[coarse.Count];
            foreach (PoliticalMapRegion candidate in detailed)
            {
                int cell = CellOf(candidate);
                int index = cell < 0 ? -1 : component[cell] - 1;
                if (index < 0 || !StringComparer.Ordinal.Equals(candidate.Key, coarse[index].Key)) continue;
                PoliticalMapRegion? previous = anchors[index];
                if (previous is null || candidate.CellCount > previous.CellCount
                    || candidate.CellCount == previous.CellCount && (candidate.X < previous.X
                        || candidate.X == previous.X && candidate.Y < previous.Y)) anchors[index] = candidate;
            }

            var anchored = new PoliticalMapRegion[coarse.Count];
            for (int index = 0; index < coarse.Count; index++)
            {
                PoliticalMapRegion region = coarse[index];
                PoliticalMapRegion? anchor = anchors[index];
                // Keep coarse sizing/priority and every disconnected label; only its world position changes.
                anchored[index] = anchor is null || region.X == anchor.X && region.Y == anchor.Y
                    ? region : region with { X = anchor.X, Y = anchor.Y };
            }

            return anchored;

            int CellOf(PoliticalMapRegion region)
            {
                int column = (int)(region.X / grid.CellSize);
                int row = (int)(region.Y / grid.CellSize);
                return region.X < 0f || region.Y < 0f || column >= grid.Width || row >= grid.Height
                    ? -1 : row * grid.Width + column;
            }
        }
    }
}
