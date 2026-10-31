using System;
using System.Collections.Generic;
using System.Linq;

namespace DellarteDellaGuerra.Domain.PoliticalMap.Grid
{
    /// <summary>
    /// Builds the political map's settlement grid from the terrain and the settlements: every land
    /// cell goes to its nearest settlement measured over land, so a settlement's patch never reaches
    /// across an estuary or a sea loch onto the far shore.
    /// </summary>
    /// <remarks>
    /// Pure and engine-free, so the campaign runs it on a worker thread. Over the 1471 map
    /// (1120x1120 cells of 1 m, about 260k of them land) it takes a few hundred milliseconds.
    /// </remarks>
    public static class PoliticalMapGridBuilder
    {
        /// <summary>
        /// Terrain at or below this height is sea or river. It traces the 1471 map's coastline, and
        /// only three riverside towns (London, Oxford, Caernarfon) sit below it.
        /// </summary>
        public const float SeaLevel = 9.6f;

        /// <summary>Rivers narrower than this (meters) do not split a landmass when cells are matched to settlements.</summary>
        public const float RiverBridge = 3f;

        public const float DefaultCellSize = 1f;

        private static readonly double Diagonal = Math.Sqrt(2);

        public static PoliticalMapGrid Build(
            PoliticalMapHeightField terrain,
            IReadOnlyList<PoliticalMapSeed> seeds,
            float cellSize = DefaultCellSize,
            float seaLevel = SeaLevel,
            float riverBridge = RiverBridge)
        {
            if (seeds.Count >= ushort.MaxValue)
            {
                throw new ArgumentException($"{seeds.Count} settlements do not fit a 16-bit cell.", nameof(seeds));
            }

            int side = (int)Math.Round(terrain.MapSize / cellSize);
            bool[] land = LandMask(terrain, side, cellSize, seaLevel);
            int bridge = Math.Max(1, (int)Math.Round(riverBridge / cellSize / 2, MidpointRounding.ToEven));
            bool[] landmass = Close(land, side, bridge);
            int[] owner = AssignOverLand(landmass, side, seeds.Select(seed => NearestCell(landmass, side, seed.Y / cellSize, seed.X / cellSize)));

            var cells = new ushort[side * side];
            for (int cell = 0; cell < cells.Length; cell++)
            {
                // A river bridged only to carry the search across stays water on the map.
                if (land[cell] && owner[cell] >= 0) cells[cell] = (ushort)(owner[cell] + 1);
            }

            return new PoliticalMapGrid(side, side, cellSize, seeds.Select(seed => seed.SettlementId).ToList(), cells);
        }

        /// <summary>Land is every cell whose centre stands above the sea.</summary>
        private static bool[] LandMask(PoliticalMapHeightField terrain, int side, float cellSize, float seaLevel)
        {
            var land = new bool[side * side];
            for (int row = 0; row < side; row++)
            {
                double y = (row + 0.5) * cellSize;
                for (int column = 0; column < side; column++)
                {
                    land[row * side + column] = terrain.HeightAt((column + 0.5) * cellSize, y) > seaLevel;
                }
            }

            return land;
        }

        /// <summary>
        /// A morphological closing with the 4-neighbour cross, <paramref name="iterations"/> times
        /// each way: water narrower than about twice that is filled. Off the grid counts as water,
        /// so the outermost cells erode away.
        /// </summary>
        private static bool[] Close(bool[] mask, int side, int iterations)
        {
            bool[] result = mask;
            for (int i = 0; i < iterations; i++) result = Step(result, side, dilate: true);
            for (int i = 0; i < iterations; i++) result = Step(result, side, dilate: false);
            return result;
        }

        private static bool[] Step(bool[] mask, int side, bool dilate)
        {
            var next = new bool[mask.Length];
            for (int row = 0; row < side; row++)
            {
                for (int column = 0; column < side; column++)
                {
                    int cell = row * side + column;
                    bool up = row + 1 < side && mask[cell + side];
                    bool down = row > 0 && mask[cell - side];
                    bool right = column + 1 < side && mask[cell + 1];
                    bool left = column > 0 && mask[cell - 1];
                    next[cell] = dilate
                        ? mask[cell] || up || down || right || left
                        : mask[cell] && up && down && right && left;
                }
            }

            return next;
        }

        /// <summary>
        /// The landmass cell nearest a point given in cell units, or -1 when there is no land. A
        /// town standing in its river so still claims the banks around it.
        /// </summary>
        private static int NearestCell(bool[] landmass, int side, double row, double column)
        {
            int centreRow = (int)Math.Round(row);
            int centreColumn = (int)Math.Round(column);
            int best = -1;
            double bestDistance = double.MaxValue;

            // Rings of growing radius around the point; ring r lies at least r - 1 cells away.
            for (int radius = 0; radius < 2 * side; radius++)
            {
                if (best >= 0 && radius - 1 > bestDistance) break;

                for (int r = centreRow - radius; r <= centreRow + radius; r++)
                {
                    if (r < 0 || r >= side) continue;
                    bool edgeRow = r == centreRow - radius || r == centreRow + radius;
                    for (int c = centreColumn - radius; c <= centreColumn + radius; c += edgeRow ? 1 : 2 * Math.Max(radius, 1))
                    {
                        if (c < 0 || c >= side || !landmass[r * side + c]) continue;

                        double distance = Math.Sqrt((r - row) * (r - row) + (c - column) * (c - column));
                        if (distance < bestDistance)
                        {
                            bestDistance = distance;
                            best = r * side + c;
                        }
                    }
                }
            }

            return best;
        }

        /// <summary>
        /// A multi-source Dijkstra over the 8-connected landmass, diagonals weighing √2: each
        /// reached cell takes the index of the seed it is nearest to. Where two seeds start on the
        /// same cell, the first one keeps it.
        /// </summary>
        private static int[] AssignOverLand(bool[] landmass, int side, IEnumerable<int> seedCells)
        {
            var distance = new double[landmass.Length];
            var owner = new int[landmass.Length];
            for (int cell = 0; cell < owner.Length; cell++)
            {
                distance[cell] = double.PositiveInfinity;
                owner[cell] = -1;
            }

            var queue = new CellQueue();
            int seedIndex = 0;
            foreach (int seedCell in seedCells)
            {
                if (seedCell >= 0 && owner[seedCell] < 0)
                {
                    owner[seedCell] = seedIndex;
                    distance[seedCell] = 0;
                    queue.Push(0, seedCell);
                }

                seedIndex++;
            }

            while (queue.Count > 0)
            {
                queue.Pop(out double reached, out int cell);
                if (reached > distance[cell]) continue;

                int row = cell / side;
                int column = cell - row * side;
                for (int dr = -1; dr <= 1; dr++)
                {
                    int r = row + dr;
                    if (r < 0 || r >= side) continue;
                    for (int dc = -1; dc <= 1; dc++)
                    {
                        int c = column + dc;
                        if ((dr == 0 && dc == 0) || c < 0 || c >= side) continue;

                        int next = r * side + c;
                        if (!landmass[next]) continue;

                        double through = reached + (dr != 0 && dc != 0 ? Diagonal : 1);
                        if (through < distance[next])
                        {
                            distance[next] = through;
                            owner[next] = owner[cell];
                            queue.Push(through, next);
                        }
                    }
                }
            }

            return owner;
        }

        /// <summary>A binary min-heap of (distance, cell), with stale entries skipped by the caller.</summary>
        private sealed class CellQueue
        {
            private double[] _keys = new double[1024];
            private int[] _cells = new int[1024];

            public int Count { get; private set; }

            public void Push(double key, int cell)
            {
                if (Count == _keys.Length)
                {
                    Array.Resize(ref _keys, Count * 2);
                    Array.Resize(ref _cells, Count * 2);
                }

                int i = Count++;
                while (i > 0)
                {
                    int parent = (i - 1) / 2;
                    if (_keys[parent] <= key) break;
                    _keys[i] = _keys[parent];
                    _cells[i] = _cells[parent];
                    i = parent;
                }

                _keys[i] = key;
                _cells[i] = cell;
            }

            public void Pop(out double key, out int cell)
            {
                key = _keys[0];
                cell = _cells[0];
                double lastKey = _keys[--Count];
                int lastCell = _cells[Count];

                int i = 0;
                while (true)
                {
                    int child = 2 * i + 1;
                    if (child >= Count) break;
                    if (child + 1 < Count && _keys[child + 1] < _keys[child]) child++;
                    if (_keys[child] >= lastKey) break;
                    _keys[i] = _keys[child];
                    _cells[i] = _cells[child];
                    i = child;
                }

                _keys[i] = lastKey;
                _cells[i] = lastCell;
            }
        }
    }
}
