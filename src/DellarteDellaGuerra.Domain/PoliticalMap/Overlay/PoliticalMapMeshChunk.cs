using System.Collections.Generic;

namespace DellarteDellaGuerra.Domain.PoliticalMap.Overlay
{
    /// <summary>A rectangle of grid cells in one colour, in cells from the map origin.</summary>
    public record PoliticalMapQuad(int Column, int Row, int Width, int Height, uint Color);

    /// <summary>One mesh's worth of quads, at most <see cref="PoliticalMapMesher.MaxQuadsPerChunk"/>.</summary>
    public record PoliticalMapMeshChunk(IReadOnlyList<PoliticalMapQuad> Quads);
}
