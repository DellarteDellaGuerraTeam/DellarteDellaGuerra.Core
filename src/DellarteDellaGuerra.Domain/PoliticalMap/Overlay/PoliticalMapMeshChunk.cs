using System;
using System.Collections.Generic;

namespace DellarteDellaGuerra.Domain.PoliticalMap.Overlay
{
    /// <summary>A rectangle of grid cells in one colour, in cells from the map origin.</summary>
    public record PoliticalMapQuad(int Column, int Row, int Width, int Height, uint Color);

    /// <summary>A point in cell coordinates, including smoothed boundary and sheet intersection points.</summary>
    public readonly struct PoliticalMapPoint
    {
        public PoliticalMapPoint(float column, float row)
        {
            Column = column;
            Row = row;
        }

        public float Column { get; }
        public float Row { get; }
    }

    /// <summary>A uniformly coloured triangle; colours never interpolate across a political boundary.</summary>
    public record PoliticalMapTriangle(PoliticalMapPoint A, PoliticalMapPoint B, PoliticalMapPoint C, uint Color);

    /// <summary>One mesh's worth of rectangles and corner geometry, at most 512 triangles in total.</summary>
    public record PoliticalMapMeshChunk(IReadOnlyList<PoliticalMapQuad> Quads)
    {
        public IReadOnlyList<PoliticalMapTriangle> Triangles { get; init; } = Array.Empty<PoliticalMapTriangle>();
        public int TriangleCount => Quads.Count * 2 + Triangles.Count;
    }
}
