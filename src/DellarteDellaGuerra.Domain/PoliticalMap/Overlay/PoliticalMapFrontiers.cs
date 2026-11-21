using System;
using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.PoliticalMap.Grid;

namespace DellarteDellaGuerra.Domain.PoliticalMap.Overlay
{
    /// <summary>One shared, relaxed boundary network for all paints, independent of terrain blocks.</summary>
    public sealed class PoliticalMapFrontiers
    {
        internal const int Iterations = 16;
        private const float MaxOffset = 0.49f;
        private readonly int _stride;
        private readonly PoliticalMapPoint[] _offsets;
        private readonly PoliticalMapGrid _grid;
        private readonly PoliticalMapSettlementPaint[] _paints;

        internal PoliticalMapFrontiers(PoliticalMapGrid grid, IReadOnlyList<PoliticalMapSettlementPaint> paints)
        {
            _grid = grid;
            _paints = paints.ToArray();
            _stride = grid.Width + 1;
            _offsets = new PoliticalMapPoint[_stride * (grid.Height + 1)];
            var next = new PoliticalMapPoint[_offsets.Length];
            var edges = new Dictionary<int, List<int>>();
            var keys = new int[paints.Count + 1];
            var colors = new Dictionary<(uint, uint, string?), int>();
            for (int i = 0; i < paints.Count; i++)
            {
                PoliticalMapSettlementPaint paint = paints[i];
                if (paint.Fill == 0) continue;
                var color = (paint.Fill, paint.Hatch, paint.RegionKey);
                if (!colors.TryGetValue(color, out int key)) colors[color] = key = colors.Count + 1;
                keys[i + 1] = key;
            }

            for (int row = 0; row < grid.Height; row++)
            {
                for (int column = 0; column < grid.Width; column++)
                {
                    int cell = row * grid.Width + column;
                    int vertex = row * _stride + column;
                    if (column > 0 && keys[grid.Cells[cell]] != keys[grid.Cells[cell - 1]])
                        Edge(vertex, vertex + _stride);
                    if (row > 0 && keys[grid.Cells[cell]] != keys[grid.Cells[cell - grid.Width]])
                        Edge(vertex, vertex + 1);
                }
            }

            for (int pass = 0; pass < Iterations; pass++)
            {
                foreach (KeyValuePair<int, List<int>> edge in edges)
                {
                    int vertex = edge.Key, column = vertex % _stride, row = vertex / _stride;
                    // Junctions and map edges are common anchors for every incident territory.
                    if (edge.Value.Count != 2 || column == 0 || row == 0 || column == grid.Width || row == grid.Height) continue;
                    PoliticalMapPoint a = At(edge.Value[0]), b = At(edge.Value[1]), current = _offsets[vertex];
                    float dx = current.Column * 0.5f + ((a.Column + b.Column) * 0.5f - column) * 0.5f;
                    float dy = current.Row * 0.5f + ((a.Row + b.Row) * 0.5f - row) * 0.5f;
                    float length = (float)Math.Sqrt(dx * dx + dy * dy);
                    // Below half a cell, edges cannot cross a cell centre or close a narrow river.
                    if (length > MaxOffset) { dx *= MaxOffset / length; dy *= MaxOffset / length; }
                    next[vertex] = new PoliticalMapPoint(dx, dy);
                }

                PoliticalMapPoint[] old = _offsets;
                _offsets = next;
                next = old;
            }

            void Edge(int first, int second)
            {
                if (!edges.TryGetValue(first, out List<int>? a)) edges[first] = a = new List<int>(2);
                if (!edges.TryGetValue(second, out List<int>? b)) edges[second] = b = new List<int>(2);
                a.Add(second);
                b.Add(first);
            }
        }

        internal PoliticalMapPoint At(int vertex) => new(vertex % _stride + _offsets[vertex].Column,
            vertex / _stride + _offsets[vertex].Row);

        internal bool Matches(IReadOnlyList<PoliticalMapSettlementPaint> paints)
        {
            if (_paints.Length != paints.Count) return false;
            for (int i = 0; i < paints.Count; i++)
                if (!_paints[i].HasSameGeometry(paints[i])) return false;
            return true;
        }

        /// <summary>Finds the painted settlement and political identity in this mesh revision.</summary>
        public (string SettlementId, string? Key)? HitTest(float x, float y)
        {
            var point = new PoliticalMapPoint(x / _grid.CellSize, y / _grid.CellSize);
            if (!(point.Column >= 0 && point.Column < _grid.Width && point.Row >= 0 && point.Row < _grid.Height))
                return null;

            int column = (int)point.Column, row = (int)point.Row;
            // Smoothing moves each corner less than half a cell. Only the eight neighbours can overlap.
            if (HitCell(column, row) is { } centre) return centre;
            for (int r = Math.Max(0, row - 1); r <= Math.Min(_grid.Height - 1, row + 1); r++)
            for (int c = Math.Max(0, column - 1); c <= Math.Min(_grid.Width - 1, column + 1); c++)
                if ((r != row || c != column) && HitCell(c, r) is { } hit) return hit;
            return null;

            (string, string?)? HitCell(int cellColumn, int cellRow)
            {
                int index = _grid.At(cellColumn, cellRow) - 1;
                if (index < 0 || _paints[index].Fill == 0) return null;
                int vertex = cellRow * _stride + cellColumn;
                PoliticalMapPoint a = At(vertex), b = At(vertex + 1), c = At(vertex + _stride + 1), d = At(vertex + _stride);
                bool inside = UsesFirstDiagonal(a, b, c, d)
                    ? InsideTriangle(a, b, c, point) || InsideTriangle(a, c, d, point)
                    : InsideTriangle(a, b, d, point) || InsideTriangle(b, c, d, point);
                return inside ? (_grid.SettlementIds[index], _paints[index].FrontierKey) : null;
            }
        }

        private static bool InsideTriangle(PoliticalMapPoint a, PoliticalMapPoint b, PoliticalMapPoint c, PoliticalMapPoint point) =>
            Cross(a, b, point) >= 0 && Cross(b, c, point) >= 0 && Cross(c, a, point) >= 0;

        private static bool UsesFirstDiagonal(PoliticalMapPoint a, PoliticalMapPoint b, PoliticalMapPoint c, PoliticalMapPoint d) =>
            Cross(a, b, c) > 0 && Cross(a, c, d) > 0;

        internal bool AddCell(List<PoliticalMapTriangle> triangles, int column, int row, uint color)
        {
            int vertex = row * _stride + column;
            if (!Moved(vertex) && !Moved(vertex + 1) && !Moved(vertex + _stride) && !Moved(vertex + _stride + 1)) return false;
            if (color == 0) return true;
            PoliticalMapPoint a = At(vertex), b = At(vertex + 1), c = At(vertex + _stride + 1), d = At(vertex + _stride);
            if (UsesFirstDiagonal(a, b, c, d))
            {
                AddTriangle(triangles, a, b, c, color);
                AddTriangle(triangles, a, c, d, color);
            }
            else
            {
                AddTriangle(triangles, a, b, d, color);
                AddTriangle(triangles, b, c, d, color);
            }
            return true;
        }

        private bool Moved(int vertex) => _offsets[vertex].Column != 0 || _offsets[vertex].Row != 0;

        // Split at sheet lines after smoothing; the frontier itself is never pinned to those lines.
        internal static void AddTriangle(List<PoliticalMapTriangle> triangles, PoliticalMapPoint a,
            PoliticalMapPoint b, PoliticalMapPoint c, uint color)
        {
            // Wide ribbons can fold at a short, sharp turn. Keep their winding consistent
            // before clipping, including triangles that stay entirely inside one sheet block.
            float area = Cross(a, b, c);
            if (area == 0) return;
            if (area < 0) (b, c) = (c, b);
            const int side = PoliticalMapMesher.MaxQuadSide;
            float left = Math.Min(a.Column, Math.Min(b.Column, c.Column)), right = Math.Max(a.Column, Math.Max(b.Column, c.Column));
            float bottom = Math.Min(a.Row, Math.Min(b.Row, c.Row)), top = Math.Max(a.Row, Math.Max(b.Row, c.Row));
            int x0 = (int)Math.Floor(left / side), x1 = (int)Math.Ceiling(right / side) - 1;
            int y0 = (int)Math.Floor(bottom / side), y1 = (int)Math.Ceiling(top / side) - 1;
            if (x0 == x1 && y0 == y1)
            {
                triangles.Add(new PoliticalMapTriangle(a, b, c, color));
                return;
            }

            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                var polygon = new List<PoliticalMapPoint> { a, b, c };
                polygon = Clip(polygon, true, x * side, true);
                polygon = Clip(polygon, true, (x + 1) * side, false);
                polygon = Clip(polygon, false, y * side, true);
                polygon = Clip(polygon, false, (y + 1) * side, false);
                for (int i = 1; i + 1 < polygon.Count; i++)
                    if (Cross(polygon[0], polygon[i], polygon[i + 1]) > 0)
                        triangles.Add(new PoliticalMapTriangle(polygon[0], polygon[i], polygon[i + 1], color));
            }
        }

        private static List<PoliticalMapPoint> Clip(List<PoliticalMapPoint> polygon, bool vertical, float line, bool above)
        {
            var result = new List<PoliticalMapPoint>(polygon.Count + 1);
            for (int i = 0; i < polygon.Count; i++)
            {
                PoliticalMapPoint a = polygon[i], b = polygon[(i + 1) % polygon.Count];
                float da = (vertical ? a.Column : a.Row) - line, db = (vertical ? b.Column : b.Row) - line;
                if (above ? da >= 0 : da <= 0) result.Add(a);
                if (da > 0 && db < 0 || da < 0 && db > 0)
                {
                    // The two sides traverse a shared edge in opposite directions: round it once.
                    if (a.Column > b.Column || a.Column == b.Column && a.Row > b.Row) (a, b) = (b, a);
                    float start = (vertical ? a.Column : a.Row) - line;
                    float end = (vertical ? b.Column : b.Row) - line;
                    float t = start / (start - end);
                    result.Add(vertical ? new PoliticalMapPoint(line, a.Row + t * (b.Row - a.Row))
                        : new PoliticalMapPoint(a.Column + t * (b.Column - a.Column), line));
                }
            }
            return result;
        }

        private static float Cross(PoliticalMapPoint a, PoliticalMapPoint b, PoliticalMapPoint c) =>
            (b.Column - a.Column) * (c.Row - a.Row) - (b.Row - a.Row) * (c.Column - a.Column);
    }
}
