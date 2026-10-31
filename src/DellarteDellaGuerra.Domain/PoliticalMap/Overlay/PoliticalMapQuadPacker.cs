using System;
using System.Collections.Generic;

namespace DellarteDellaGuerra.Domain.PoliticalMap.Overlay
{
    /// <summary>Packs a territory's same-coloured cells into quads and the quads into mesh-sized chunks.</summary>
    internal static class PoliticalMapQuadPacker
    {
        // Greedy rectangles: from each uncovered cell, run right, then grow down while the whole run matches.
        // A quad never crosses a block line (every MaxQuadSide cells of the whole grid), so it lies on
        // one patch of the PoliticalMapSheet.
        internal static List<PoliticalMapQuad> Rectangles(uint[] colors, PoliticalMapBox box)
        {
            var quads = new List<PoliticalMapQuad>();
            var covered = new bool[colors.Length];
            for (int y = 0; y < box.Height; y++)
            {
                for (int x = 0; x < box.Width; x++)
                {
                    int start = y * box.Width + x;
                    uint color = colors[start];
                    if (color == 0 || covered[start]) continue;

                    int w = 1;
                    while (!OnBlockLine(box.Column + x + w) && x + w < box.Width && Free(start + w)) w++;

                    int h = 1;
                    while (!OnBlockLine(box.Row + y + h) && y + h < box.Height && RowFree((y + h) * box.Width + x, w)) h++;

                    for (int dy = 0; dy < h; dy++)
                    {
                        for (int dx = 0; dx < w; dx++) covered[(y + dy) * box.Width + x + dx] = true;
                    }

                    quads.Add(new PoliticalMapQuad(box.Column + x, box.Row + y, w, h, color));

                    bool Free(int i) => colors[i] == color && !covered[i];

                    bool RowFree(int rowStart, int runWidth)
                    {
                        for (int dx = 0; dx < runWidth; dx++)
                        {
                            if (!Free(rowStart + dx)) return false;
                        }

                        return true;
                    }
                }
            }

            return quads;
        }

        private static bool OnBlockLine(int gridLine) => gridLine % PoliticalMapMesher.MaxQuadSide == 0;

        internal static void AddChunks(List<PoliticalMapMeshChunk> chunks, List<PoliticalMapQuad> quads)
        {
            for (int start = 0; start < quads.Count; start += PoliticalMapMesher.MaxQuadsPerChunk)
            {
                chunks.Add(new PoliticalMapMeshChunk(quads.GetRange(start, Math.Min(PoliticalMapMesher.MaxQuadsPerChunk, quads.Count - start))));
            }
        }
    }

    internal readonly struct PoliticalMapBox
    {
        public PoliticalMapBox(int column, int row, int width, int height)
        {
            Column = column;
            Row = row;
            Width = width;
            Height = height;
        }

        public int Column { get; }
        public int Row { get; }
        public int Width { get; }
        public int Height { get; }

        public int Local(int column, int row) => (row - Row) * Width + column - Column;
    }
}
