using DellarteDellaGuerra.Domain.PoliticalMap.Grid;

namespace DellarteDellaGuerra.Domain.PoliticalMap.Port
{
    /// <summary>The campaign map's terrain heights the political map takes its coastline from.</summary>
    /// <remarks>Called off the main thread, so implementations must not touch the engine.</remarks>
    public interface IPoliticalMapTerrain
    {
        PoliticalMapHeightField Load();
    }
}
