using System;
using System.Collections.Generic;
using DellarteDellaGuerra.Domain.PoliticalMap.Grid;

namespace DellarteDellaGuerra.Domain.PoliticalMap.Overlay
{
    /// <summary>White ribbons on the shared relaxed edges. Dash phase follows a complete boundary chain.</summary>
    internal static class PoliticalMapFrontierStrokes
    {
        private const uint Colour = 0xFFFFFFFF;
        // Cell coordinates: broad enough to read at the far camera without extra geometry.
        private const float MainHalfWidth = 0.45f;
        private const float SubVassalHalfWidth = 0.3f;
        private const float Dash = 2.4f;
        private const float Period = 4f;

        internal static Dictionary<string, List<PoliticalMapTriangle>> Build(PoliticalMapGrid grid,
            IReadOnlyList<PoliticalMapSettlementPaint> paints, IReadOnlyList<string> territories, PoliticalMapFrontiers geometry)
        {
            int stride = grid.Width + 1;
            var groups = new Dictionary<(string, string, bool), List<Edge>>();
            var result = new Dictionary<string, List<PoliticalMapTriangle>>(StringComparer.Ordinal);
            for (int row = 0; row < grid.Height; row++)
            for (int column = 0; column < grid.Width; column++)
            {
                int cell = row * grid.Width + column, vertex = row * stride + column;
                if (column > 0) Boundary(cell, cell - 1, vertex, vertex + stride);
                if (row > 0) Boundary(cell, cell - grid.Width, vertex, vertex + 1);
            }

            foreach (KeyValuePair<(string, string, bool), List<Edge>> group in groups)
            {
                List<Edge> edges = group.Value;
                var incident = new Dictionary<int, List<int>>();
                for (int index = 0; index < edges.Count; index++)
                {
                    AddIncident(edges[index].A, index);
                    AddIncident(edges[index].B, index);
                }
                var used = new bool[edges.Count];
                // Open ends/junctions first, then any remaining closed loops, in grid scan order.
                for (int index = 0; index < edges.Count; index++)
                {
                    if (incident[edges[index].A].Count != 2) Walk(index, edges[index].A);
                    if (incident[edges[index].B].Count != 2) Walk(index, edges[index].B);
                }
                for (int index = 0; index < edges.Count; index++) Walk(index, edges[index].A);

                void AddIncident(int vertex, int edge)
                {
                    if (!incident.TryGetValue(vertex, out List<int>? list)) incident[vertex] = list = new List<int>(2);
                    list.Add(edge);
                }

                void Walk(int first, int start)
                {
                    if (used[first]) return;
                    var chain = new List<(int Start, int End, string Territory)>();
                    int index = first, vertex = start;
                    do
                    {
                        used[index] = true;
                        Edge edge = edges[index];
                        int end = edge.A == vertex ? edge.B : edge.A;
                        chain.Add((vertex, end, edge.Territory));
                        vertex = end;
                        if (incident[vertex].Count != 2) break;
                        index = incident[vertex][0] == index ? incident[vertex][1] : incident[vertex][0];
                    } while (!used[index]);
                    AddChain(chain, group.Key.Item3);
                }
            }
            return result;

            void Boundary(int firstCell, int secondCell, int a, int b)
            {
                int first = grid.Cells[firstCell] - 1, second = grid.Cells[secondCell] - 1;
                if (first < 0 || second < 0 || first == second) return; // No coastlines or riverbanks.
                PoliticalMapSettlementPaint x = paints[first], y = paints[second];
                if (x.Fill == 0 || y.Fill == 0 || x.FrontierKey is not { } xKey || y.FrontierKey is not { } yKey || xKey == yKey) return;
                bool dashed = x.ParentRegionKey is { } parent && parent == y.ParentRegionKey;
                var key = string.CompareOrdinal(xKey, yKey) < 0 ? (xKey, yKey, dashed) : (yKey, xKey, dashed);
                if (!groups.TryGetValue(key, out List<Edge>? edges)) groups[key] = edges = new List<Edge>();
                // Draw an edge once, even when both incident counties are rebuilt.
                string territory = string.CompareOrdinal(territories[first], territories[second]) <= 0 ? territories[first] : territories[second];
                edges.Add(new Edge(a, b, territory));
            }

            void AddChain(List<(int Start, int End, string Territory)> chain, bool dashed)
            {
                float halfWidth = dashed ? SubVassalHalfWidth : MainHalfWidth;
                var points = new PoliticalMapPoint[chain.Count + 1];
                for (int i = 0; i < chain.Count; i++) points[i] = geometry.At(chain[i].Start);
                points[chain.Count] = geometry.At(chain[chain.Count - 1].End);
                bool closed = chain[0].Start == chain[chain.Count - 1].End;
                var offsets = new PoliticalMapPoint[points.Length];
                for (int i = 0; i < offsets.Length; i++)
                {
                    PoliticalMapPoint before = points[i > 0 ? i - 1 : closed ? points.Length - 2 : 0];
                    PoliticalMapPoint after = points[i + 1 < points.Length ? i + 1 : closed ? 1 : i];
                    PoliticalMapPoint n1 = Normal(before, points[i]), n2 = Normal(points[i], after);
                    if (i == 0 && !closed) n1 = n2;
                    if (i == points.Length - 1 && !closed) n2 = n1;
                    float denominator = 1 + n1.Column * n2.Column + n1.Row * n2.Row;
                    float scale = halfWidth / Math.Max(0.5f, denominator);
                    offsets[i] = new PoliticalMapPoint((n1.Column + n2.Column) * scale, (n1.Row + n2.Row) * scale);
                }

                double distance = 0;
                for (int i = 0; i < chain.Count; i++)
                {
                    PoliticalMapPoint a = points[i], b = points[i + 1];
                    float dx = b.Column - a.Column, dy = b.Row - a.Row;
                    double length = Math.Sqrt(dx * dx + dy * dy), travelled = 0;
                    if (length < 0.00001) continue;
                    if (!result.TryGetValue(chain[i].Territory, out List<PoliticalMapTriangle>? triangles))
                        result[chain[i].Territory] = triangles = new List<PoliticalMapTriangle>();
                    while (travelled < length - 0.000001)
                    {
                        double phase = (distance + travelled) % Period;
                        bool ink = !dashed || phase < Dash;
                        double end = dashed ? Math.Min(length, travelled + (ink ? Dash : Period) - phase) : length;
                        if (ink)
                        {
                            float t0 = (float)(travelled / length), t1 = (float)(end / length);
                            PoliticalMapPoint p = Lerp(a, b, t0), q = Lerp(a, b, t1);
                            PoliticalMapPoint pOffset = Lerp(offsets[i], offsets[i + 1], t0);
                            PoliticalMapPoint qOffset = Lerp(offsets[i], offsets[i + 1], t1);
                            PoliticalMapPoint leftA = Offset(p, pOffset, 1), rightA = Offset(p, pOffset, -1);
                            PoliticalMapPoint leftB = Offset(q, qOffset, 1), rightB = Offset(q, qOffset, -1);
                            PoliticalMapFrontiers.AddTriangle(triangles, leftA, rightA, rightB, Colour);
                            PoliticalMapFrontiers.AddTriangle(triangles, leftA, rightB, leftB, Colour);
                        }
                        travelled = end;
                    }
                    distance += length;
                }
            }
        }

        private static PoliticalMapPoint Lerp(PoliticalMapPoint a, PoliticalMapPoint b, float t) =>
            new(a.Column + (b.Column - a.Column) * t, a.Row + (b.Row - a.Row) * t);
        private static PoliticalMapPoint Offset(PoliticalMapPoint p, PoliticalMapPoint normal, float sign) =>
            new(p.Column + normal.Column * sign, p.Row + normal.Row * sign);
        private static PoliticalMapPoint Normal(PoliticalMapPoint a, PoliticalMapPoint b)
        {
            float dx = b.Column - a.Column, dy = b.Row - a.Row;
            float length = (float)Math.Sqrt(dx * dx + dy * dy);
            return length < 0.00001f ? new PoliticalMapPoint(0, 0) : new PoliticalMapPoint(-dy / length, dx / length);
        }
        private sealed record Edge(int A, int B, string Territory);
    }
}
