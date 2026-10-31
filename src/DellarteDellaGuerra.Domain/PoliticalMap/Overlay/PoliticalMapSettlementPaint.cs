namespace DellarteDellaGuerra.Domain.PoliticalMap.Overlay
{
    /// <summary>
    /// How one settlement's cells are painted in a view: the washed fill (0 for none), the washed
    /// hatch colour striped across it (0 for none, which the painter now always gives), and the
    /// county, duchy and realm keys the region labels group cells by (null for none). In the owners
    /// view <see cref="RegionKey"/> is the clan whose vassal-hierarchy region the land is painted in
    /// (<see cref="PoliticalMapVassalHierarchy.RegionHolderOf"/>). Only the colours are drawn.
    /// </summary>
    public record PoliticalMapSettlementPaint(
        uint Fill, uint Hatch, string? CountyKey, string? DuchyKey, string? RealmKey, string? RegionKey = null)
    {
        /// <summary>Nothing drawn and no keys.</summary>
        public static readonly PoliticalMapSettlementPaint Blank = new(0, 0, null, null, null);
    }
}
