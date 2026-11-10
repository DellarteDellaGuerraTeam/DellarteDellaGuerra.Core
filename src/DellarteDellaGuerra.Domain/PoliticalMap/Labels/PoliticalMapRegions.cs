using System;
using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.PoliticalMap.Grid;

namespace DellarteDellaGuerra.Domain.PoliticalMap.Labels
{
    /// <summary>
    /// One contiguous block of grid cells sharing a label key, with the point its label is drawn
    /// at: the centre of the largest circle that fits inside the block (its pole of
    /// inaccessibility), so a crescent or an L is labelled on its land, never off it.
    /// </summary>
    /// <param name="Key">The grouping key the cells share (a kingdom, clan or title id).</param>
    /// <param name="CellCount">How many cells the block holds.</param>
    /// <param name="X">The pole's map x, in meters.</param>
    /// <param name="Y">The pole's map y, in meters.</param>
    /// <param name="Radius">The radius of the largest circle inside the block around the pole, in meters.</param>
    public sealed record PoliticalMapRegion(string Key, int CellCount, float X, float Y, float Radius);

    /// <summary>
    /// Cuts the grid into contiguous regions of one key (4-connected cells) and finds where each is
    /// labelled. Water, unowned land and cells whose settlement has no key belong to no region.
    /// </summary>
    /// <remarks>
    /// The pole comes from a chamfer distance transform (3-4 weights): each cell's distance to the
    /// nearest cell outside its region, the map edge counting as outside. The cell furthest in wins;
    /// among equally deep cells, the one nearest the region's centroid. A 1120x1120 grid takes a few
    /// tens of milliseconds, so callers run it off the main thread.
    /// </remarks>
    public static class PoliticalMapRegions
    {
        /// <summary>One label per title, kept on its largest land patch regardless of the viewport.</summary>
        public static IReadOnlyList<PoliticalMapRegion> LargestPatches(IEnumerable<PoliticalMapRegion> regions) =>
            regions.OrderByDescending(region => region.CellCount)
                .GroupBy(region => region.Key, StringComparer.Ordinal)
                .Select(group => group.First()).ToArray();

        /// <summary>Nearby label centres reached entirely through this same land patch.</summary>
        public static IReadOnlyList<PoliticalMapLabelAnchor> AlternativeAnchors(
            PoliticalMapGrid grid, IReadOnlyList<string?> keys, PoliticalMapRegion region)
        {
            var result = new List<PoliticalMapLabelAnchor>();
            foreach (float distance in new[] { 0.6f, 1.2f, 1.8f })
                for (int direction = 0; direction < 8; direction++)
                {
                    double angle = direction * Math.PI / 4;
                    float dx = (float)Math.Cos(angle) * region.Radius * distance;
                    float dy = (float)Math.Sin(angle) * region.Radius * distance;
                    int steps = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(dx * dx + dy * dy) / (grid.CellSize * 0.5f)));
                    bool inside = true;
                    for (int step = 1; step <= steps; step++)
                    {
                        int x = (int)Math.Floor((region.X + dx * step / steps) / grid.CellSize);
                        int y = (int)Math.Floor((region.Y + dy * step / steps) / grid.CellSize);
                        if (x < 0 || y < 0 || x >= grid.Width || y >= grid.Height) { inside = false; break; }
                        int settlement = grid.Cells[y * grid.Width + x] - 1;
                        if (settlement < 0 || settlement >= keys.Count || keys[settlement] != region.Key)
                        { inside = false; break; }
                    }
                    if (inside) result.Add(new PoliticalMapLabelAnchor(region.X + dx, region.Y + dy));
                }
            return result;
        }

        private const int Straight = 3;
        private const int Diagonal = 4;
        private const int Unreached = int.MaxValue / 2;

        /// <param name="grid">The settlement grid.</param>
        /// <param name="keyOfSettlement">The key of each settlement, by its 0-based index in the grid; null for none.</param>
        /// <param name="minCells">Regions smaller than this get no label and are left out.</param>
        /// <returns>The regions of at least <paramref name="minCells"/> cells, largest first.</returns>
        public static IReadOnlyList<PoliticalMapRegion> Find(PoliticalMapGrid grid, IReadOnlyList<string?> keyOfSettlement, int minCells)
        {
            int width = grid.Width;
            int height = grid.Height;
            int count = width * height;

            var keys = new List<string>();
            int[] cellKey = KeyCells(grid, keyOfSettlement, keys);

            // Connected components, flood-filled with an explicit stack.
            var component = new int[count];
            for (int i = 0; i < count; i++) component[i] = -1;
            var stack = new int[count];
            var componentKey = new List<int>();
            var componentCells = new List<int>();
            var sumColumn = new List<long>();
            var sumRow = new List<long>();
            for (int start = 0; start < count; start++)
            {
                if (cellKey[start] < 0 || component[start] >= 0) continue;

                int id = componentKey.Count;
                int key = cellKey[start];
                int cells = 0;
                long columns = 0, rows = 0;
                int top = 0;
                stack[top++] = start;
                component[start] = id;
                while (top > 0)
                {
                    int cell = stack[--top];
                    int column = cell % width;
                    int row = cell / width;
                    cells++;
                    columns += column;
                    rows += row;
                    if (column > 0) Push(cell - 1);
                    if (column < width - 1) Push(cell + 1);
                    if (row > 0) Push(cell - width);
                    if (row < height - 1) Push(cell + width);
                }

                componentKey.Add(key);
                componentCells.Add(cells);
                sumColumn.Add(columns);
                sumRow.Add(rows);

                void Push(int next)
                {
                    if (component[next] >= 0 || cellKey[next] != key) return;
                    component[next] = id;
                    stack[top++] = next;
                }
            }

            int[] depth = DistanceToOutside(component, width, height);

            // The deepest cell of each component, ties to the one nearest the centroid.
            int components = componentKey.Count;
            var deepest = new int[components];
            var pole = new int[components];
            var poleCentroidDistance = new double[components];
            for (int c = 0; c < components; c++) pole[c] = -1;
            for (int cell = 0; cell < count; cell++)
            {
                int c = component[cell];
                if (c >= 0 && depth[cell] > deepest[c]) deepest[c] = depth[cell];
            }

            for (int cell = 0; cell < count; cell++)
            {
                int c = component[cell];
                if (c < 0 || depth[cell] != deepest[c] || componentCells[c] < minCells) continue;

                double dx = cell % width - (double)sumColumn[c] / componentCells[c];
                double dy = cell / width - (double)sumRow[c] / componentCells[c];
                double distance = dx * dx + dy * dy;
                if (pole[c] < 0 || distance < poleCentroidDistance[c])
                {
                    pole[c] = cell;
                    poleCentroidDistance[c] = distance;
                }
            }

            var regions = new List<PoliticalMapRegion>();
            for (int c = 0; c < components; c++)
            {
                if (pole[c] < 0) continue;

                float size = grid.CellSize;
                regions.Add(new PoliticalMapRegion(
                    keys[componentKey[c]],
                    componentCells[c],
                    (pole[c] % width + 0.5f) * size,
                    (pole[c] / width + 0.5f) * size,
                    Math.Max(0.5f, deepest[c] / (float)Straight - 0.5f) * size));
            }

            regions.Sort((a, b) => a.CellCount != b.CellCount
                ? b.CellCount.CompareTo(a.CellCount)
                : a.X != b.X ? a.X.CompareTo(b.X) : a.Y.CompareTo(b.Y));
            return regions;
        }

        private static int[] KeyCells(PoliticalMapGrid grid, IReadOnlyList<string?> keyOfSettlement, List<string> keys)
        {
            var keyIds = new Dictionary<string, int>(StringComparer.Ordinal);
            var settlementKey = new int[grid.SettlementIds.Count];
            for (int s = 0; s < settlementKey.Length; s++)
            {
                string? key = s < keyOfSettlement.Count ? keyOfSettlement[s] : null;
                if (key is null)
                {
                    settlementKey[s] = -1;
                    continue;
                }

                if (!keyIds.TryGetValue(key, out int id))
                {
                    keyIds[key] = id = keys.Count;
                    keys.Add(key);
                }

                settlementKey[s] = id;
            }

            ushort[] cells = grid.Cells;
            var cellKey = new int[cells.Length];
            for (int i = 0; i < cells.Length; i++)
            {
                ushort cell = cells[i];
                cellKey[i] = cell == PoliticalMapGrid.None || cell > settlementKey.Length ? -1 : settlementKey[cell - 1];
            }

            return cellKey;
        }

        // Two chamfer passes; a neighbour in another component, or off the map, is outside.
        // The result is in thirds of a cell, measured centre to centre.
        private static int[] DistanceToOutside(int[] component, int width, int height)
        {
            var depth = new int[component.Length];
            for (int i = 0; i < depth.Length; i++) depth[i] = component[i] < 0 ? 0 : Unreached;

            for (int row = 0; row < height; row++)
            {
                for (int column = 0; column < width; column++)
                {
                    int cell = row * width + column;
                    int c = component[cell];
                    if (c < 0) continue;

                    int best = depth[cell];
                    best = Math.Min(best, Step(column - 1, row, Straight));
                    best = Math.Min(best, Step(column - 1, row - 1, Diagonal));
                    best = Math.Min(best, Step(column, row - 1, Straight));
                    best = Math.Min(best, Step(column + 1, row - 1, Diagonal));
                    depth[cell] = best;

                    int Step(int x, int y, int weight)
                    {
                        if (x < 0 || y < 0 || x >= width || y >= height) return weight;
                        int next = y * width + x;
                        return component[next] != c ? weight : depth[next] + weight;
                    }
                }
            }

            for (int row = height - 1; row >= 0; row--)
            {
                for (int column = width - 1; column >= 0; column--)
                {
                    int cell = row * width + column;
                    int c = component[cell];
                    if (c < 0) continue;

                    int best = depth[cell];
                    best = Math.Min(best, Step(column + 1, row, Straight));
                    best = Math.Min(best, Step(column + 1, row + 1, Diagonal));
                    best = Math.Min(best, Step(column, row + 1, Straight));
                    best = Math.Min(best, Step(column - 1, row + 1, Diagonal));
                    depth[cell] = best;

                    int Step(int x, int y, int weight)
                    {
                        if (x < 0 || y < 0 || x >= width || y >= height) return weight;
                        int next = y * width + x;
                        return component[next] != c ? weight : depth[next] + weight;
                    }
                }
            }

            return depth;
        }
    }
}
