using System;
using System.Collections.Generic;
using DellarteDellaGuerra.Domain.PoliticalMap.Grid;

namespace DellarteDellaGuerra.Domain.PoliticalMap.Overlay
{
    /// <summary>
    /// Turns a territory's cells into the quads its meshes are built from: every cell in its
    /// settlement's fill colour, striped with the hatch colour where it has one. There are no
    /// borders: neighbouring regions are told apart by their colours alone.
    /// </summary>
    /// <remarks>
    /// Same-coloured cells merge into rectangles of at most <see cref="MaxQuadSide"/> cells a side,
    /// so a quad never strays far from the terrain under its corners, and a chunk holds at most 256
    /// quads, the 512 triangles a mesh is capped at.
    /// </remarks>
    public sealed class PoliticalMapMesher
    {
        public const int MaxQuadSide = 8;
        public const int MaxQuadsPerChunk = 256;

        private readonly PoliticalMapGrid _grid;
        private readonly Dictionary<string, List<int>> _cellsByTerritory = new();

        /// <param name="grid">The settlement grid.</param>
        /// <param name="territoryOfSettlement">The territory each grid settlement is meshed with, by 0-based index.</param>
        public PoliticalMapMesher(PoliticalMapGrid grid, IReadOnlyList<string> territoryOfSettlement)
        {
            _grid = grid;
            for (int index = 0; index < grid.Cells.Length; index++)
            {
                ushort cell = grid.Cells[index];
                if (cell == PoliticalMapGrid.None) continue;

                string territory = territoryOfSettlement[cell - 1];
                if (!_cellsByTerritory.TryGetValue(territory, out List<int>? cells))
                {
                    _cellsByTerritory[territory] = cells = new List<int>();
                }

                cells.Add(index);
            }
        }

        /// <summary>Every territory holding at least one cell.</summary>
        public IEnumerable<string> Territories => _cellsByTerritory.Keys;

        /// <summary>The territory's chunks, painted from each grid settlement's paint (by 0-based index).</summary>
        public IReadOnlyList<PoliticalMapMeshChunk> Build(string territory, IReadOnlyList<PoliticalMapSettlementPaint> paints)
        {
            var chunks = new List<PoliticalMapMeshChunk>();
            if (!_cellsByTerritory.TryGetValue(territory, out List<int>? cells)) return chunks;

            int width = _grid.Width;
            int minColumn = int.MaxValue, minRow = int.MaxValue, maxColumn = int.MinValue, maxRow = int.MinValue;
            foreach (int index in cells)
            {
                int column = index % width, row = index / width;
                minColumn = Math.Min(minColumn, column);
                maxColumn = Math.Max(maxColumn, column);
                minRow = Math.Min(minRow, row);
                maxRow = Math.Max(maxRow, row);
            }

            var box = new PoliticalMapBox(minColumn, minRow, maxColumn - minColumn + 1, maxRow - minRow + 1);
            var fill = new uint[box.Width * box.Height];
            foreach (int index in cells)
            {
                int column = index % width, row = index / width;
                PoliticalMapSettlementPaint paint = paints[_grid.Cells[index] - 1];
                fill[box.Local(column, row)] = paint.Hatch != 0 && (column + row) / 4 % 2 == 1 ? paint.Hatch : paint.Fill;
            }

            PoliticalMapQuadPacker.AddChunks(chunks, PoliticalMapQuadPacker.Rectangles(fill, box));
            return chunks;
        }
    }
}
