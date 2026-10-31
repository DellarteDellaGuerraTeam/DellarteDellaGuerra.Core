using System;
using DellarteDellaGuerra.Domain.PoliticalMap.Grid;

namespace DellarteDellaGuerra.Domain.PoliticalMap.Overlay
{
    /// <summary>
    /// The surface the political map is drawn on: a sheet draped over the terrain rather than laid
    /// on it. The grid is cut into blocks of <see cref="PoliticalMapMesher.MaxQuadSide"/> cells; the
    /// sheet's height at each block corner is the highest terrain over the blocks touching that
    /// corner, and between corners it is interpolated bilinearly.
    /// </summary>
    /// <remarks>
    /// Over a block the sheet never dips below the block's highest terrain, so a quad that stays
    /// inside one block covers its hills. Along any grid line inside a block the sheet is a straight
    /// line, so a small quad's corner lies exactly on the edge of the big quad beside it and quads of
    /// mixed sizes meet without cracks.
    /// </remarks>
    public sealed class PoliticalMapSheet
    {
        private readonly int _step;
        private readonly int _nodesWide;
        private readonly int _nodesHigh;
        private readonly float[] _nodes;

        public PoliticalMapSheet(PoliticalMapHeightField terrain, PoliticalMapGrid grid, int step = PoliticalMapMesher.MaxQuadSide)
        {
            if (step < 1) throw new ArgumentOutOfRangeException(nameof(step), step, "A block is at least one cell.");

            _step = step;
            int blocksWide = (grid.Width + step - 1) / step;
            int blocksHigh = (grid.Height + step - 1) / step;
            var blocks = new float[blocksWide * blocksHigh];
            for (int row = 0; row < blocksHigh; row++)
            {
                for (int column = 0; column < blocksWide; column++)
                {
                    blocks[row * blocksWide + column] = HighestTerrain(
                        terrain,
                        column * step * grid.CellSize,
                        row * step * grid.CellSize,
                        Math.Min((column + 1) * step, grid.Width) * grid.CellSize,
                        Math.Min((row + 1) * step, grid.Height) * grid.CellSize);
                }
            }

            _nodesWide = blocksWide + 1;
            _nodesHigh = blocksHigh + 1;
            _nodes = new float[_nodesWide * _nodesHigh];
            for (int row = 0; row < _nodesHigh; row++)
            {
                for (int column = 0; column < _nodesWide; column++)
                {
                    float highest = float.MinValue;
                    for (int blockRow = Math.Max(row - 1, 0); blockRow <= Math.Min(row, blocksHigh - 1); blockRow++)
                    {
                        for (int blockColumn = Math.Max(column - 1, 0); blockColumn <= Math.Min(column, blocksWide - 1); blockColumn++)
                        {
                            highest = Math.Max(highest, blocks[blockRow * blocksWide + blockColumn]);
                        }
                    }

                    _nodes[row * _nodesWide + column] = highest;
                }
            }
        }

        /// <summary>The sheet's height in cell coordinates, including fractional boundary points.</summary>
        public float HeightAt(float column, float row)
        {
            int nodeColumn = Math.Min((int)(column / _step), _nodesWide - 2);
            int nodeRow = Math.Min((int)(row / _step), _nodesHigh - 2);
            float dx = (column - nodeColumn * _step) / (float)_step;
            float dy = (row - nodeRow * _step) / (float)_step;

            float bottom = Node(nodeColumn, nodeRow) * (1 - dx) + Node(nodeColumn + 1, nodeRow) * dx;
            float top = Node(nodeColumn, nodeRow + 1) * (1 - dx) + Node(nodeColumn + 1, nodeRow + 1) * dx;
            return bottom * (1 - dy) + top * dy;
        }

        private float Node(int column, int row) => _nodes[row * _nodesWide + column];

        // Between samples the terrain is drawn as straight lines, so its highest point over a
        // rectangle is its highest sample on or just outside the rectangle.
        private static float HighestTerrain(PoliticalMapHeightField terrain, float left, float bottom, float right, float top)
        {
            double scale = (terrain.Side - 1) / (double)terrain.MapSize;
            int column0 = Index(Math.Floor(left * scale)), column1 = Index(Math.Ceiling(right * scale));
            int row0 = Index(Math.Floor(bottom * scale)), row1 = Index(Math.Ceiling(top * scale));
            ushort highest = 0;
            for (int row = row0; row <= row1; row++)
            {
                for (int column = column0; column <= column1; column++)
                {
                    highest = Math.Max(highest, terrain.Samples[row * terrain.Side + column]);
                }
            }

            return terrain.MinHeight + highest / 65535f * (terrain.MaxHeight - terrain.MinHeight);

            int Index(double index) => (int)Math.Max(0, Math.Min(terrain.Side - 1, index));
        }
    }
}
