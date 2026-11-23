using System.Collections.Generic;

namespace DellarteDellaGuerra.Domain.PoliticalMap.Grid
{
    /// <summary>
    /// The campaign map cut into square cells, each owned by the settlement nearest to it over
    /// land. Water, and land no settlement can reach (Ireland), belong to no one. Counties, duchies
    /// and realms are grouped from these cells through the title tree, never stored here.
    /// </summary>
    public sealed class PoliticalMapGrid
    {
        public const ushort None = 0;

        public PoliticalMapGrid(int width, int height, float cellSize, IReadOnlyList<string> settlementIds, ushort[] cells)
        {
            Width = width;
            Height = height;
            CellSize = cellSize;
            SettlementIds = settlementIds;
            Cells = cells;
        }

        public int Width { get; }

        public int Height { get; }

        /// <summary>A cell's edge in meters. Cell (0, 0) has its corner at the map origin.</summary>
        public float CellSize { get; }

        /// <summary>The settlements cells point at: a cell value of n is <c>SettlementIds[n - 1]</c>.</summary>
        public IReadOnlyList<string> SettlementIds { get; }

        /// <summary>Row-major from the map's y = 0 edge up; <see cref="None"/> or a 1-based settlement index.</summary>
        public ushort[] Cells { get; }

        public ushort At(int column, int row) => Cells[row * Width + column];

        /// <summary>The settlement owning the cell under a map position, or null for water, unowned land and off-map.</summary>
        public string? SettlementAt(float x, float y)
        {
            int column = (int)(x / CellSize);
            int row = (int)(y / CellSize);
            if (x < 0 || y < 0 || column >= Width || row >= Height) return null;

            ushort cell = At(column, row);
            return cell == None ? null : SettlementIds[cell - 1];
        }
    }
}
