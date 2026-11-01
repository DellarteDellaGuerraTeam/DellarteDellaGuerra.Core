using System;
using System.Collections.Generic;
using DellarteDellaGuerra.Domain.PoliticalMap.Grid;

namespace DellarteDellaGuerra.Domain.PoliticalMap.Overlay
{
    /// <summary>
    /// Turns a territory's cells into the geometry its meshes are built from: every cell in its
    /// settlement's fill colour, with thin shared frontiers between displayed political regions.
    /// </summary>
    /// <remarks>
    /// Same-coloured cells merge into rectangles of at most <see cref="MaxQuadSide"/> cells a side,
    /// so a quad never strays far from the terrain under its corners, and a chunk holds at most 256
    /// quads, the 512 triangles a mesh is capped at. All paints use one relaxed boundary network;
    /// its cell geometry is split at terrain block lines without changing the ownership grid.
    /// </remarks>
    public sealed class PoliticalMapMesher
    {
        public const int MaxQuadSide = 8;
        public const int MaxQuadsPerChunk = 256;
        public const int MaxTrianglesPerChunk = MaxQuadsPerChunk * 2;

        private readonly PoliticalMapGrid _grid;
        private readonly Dictionary<string, List<int>> _cellsByTerritory = new();
        private readonly IReadOnlyList<string> _territoryOfSettlement;
        public const int SmoothingReach = PoliticalMapFrontiers.Iterations + 2;
        private PoliticalMapFrontiers? _frontiers;
        private PoliticalMapSettlementPaint[] _frontierPaints = Array.Empty<PoliticalMapSettlementPaint>();
        private Dictionary<string, List<PoliticalMapTriangle>> _strokes = new();

        /// <param name="grid">The settlement grid.</param>
        /// <param name="territoryOfSettlement">The territory each grid settlement is meshed with, by 0-based index.</param>
        public PoliticalMapMesher(PoliticalMapGrid grid, IReadOnlyList<string> territoryOfSettlement)
        {
            _grid = grid;
            _territoryOfSettlement = territoryOfSettlement;
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
        /// <remarks>A paint change requires rebuilding territories within <see cref="SmoothingReach"/> cells.</remarks>
        public IReadOnlyList<PoliticalMapMeshChunk> Build(string territory, IReadOnlyList<PoliticalMapSettlementPaint> paints)
        {
            var chunks = new List<PoliticalMapMeshChunk>();
            if (!_cellsByTerritory.TryGetValue(territory, out List<int>? cells)) return chunks;
            UpdateFrontiers(paints);

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
            var triangles = new List<PoliticalMapTriangle>();
            foreach (int index in cells)
            {
                int column = index % width, row = index / width;
                PoliticalMapSettlementPaint paint = paints[_grid.Cells[index] - 1];
                uint color = paint.Hatch != 0 && (column + row) / 4 % 2 == 1 ? paint.Hatch : paint.Fill;
                if (!_frontiers!.AddCell(triangles, column, row, color)) fill[box.Local(column, row)] = color;
            }

            PoliticalMapQuadPacker.AddChunks(chunks, PoliticalMapQuadPacker.Rectangles(fill, box), triangles);
            if (_strokes.TryGetValue(territory, out List<PoliticalMapTriangle>? strokes))
            {
                var strokeChunks = new List<PoliticalMapMeshChunk>();
                PoliticalMapQuadPacker.AddChunks(strokeChunks, new List<PoliticalMapQuad>(), strokes);
                foreach (PoliticalMapMeshChunk chunk in strokeChunks) chunks.Add(chunk with { IsFrontier = true });
            }
            return chunks;
        }

        private void UpdateFrontiers(IReadOnlyList<PoliticalMapSettlementPaint> paints)
        {
            bool changed = _frontiers is null || _frontierPaints.Length != paints.Count;
            for (int i = 0; !changed && i < paints.Count; i++)
                changed = !_frontierPaints[i].HasSameGeometry(paints[i]);
            if (!changed) return;

            _frontiers = new PoliticalMapFrontiers(_grid, paints);
            _strokes = PoliticalMapFrontierStrokes.Build(_grid, paints, _territoryOfSettlement, _frontiers);
            _frontierPaints = new PoliticalMapSettlementPaint[paints.Count];
            for (int i = 0; i < paints.Count; i++) _frontierPaints[i] = paints[i];
        }
    }
}
