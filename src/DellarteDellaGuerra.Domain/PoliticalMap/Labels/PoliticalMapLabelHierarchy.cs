using DellarteDellaGuerra.Domain.PoliticalMap.Overlay;
using DellarteDellaGuerra.Domain.Titles.Model;

namespace DellarteDellaGuerra.Domain.PoliticalMap.Labels
{
    public static class PoliticalMapLabelHierarchy
    {
        public static bool Shows(PoliticalMapLabelTier tier, TitleRank? rank, PoliticalMapOwnerLevel level) =>
            tier != PoliticalMapLabelTier.Owner || level == PoliticalMapOwnerLevel.SubVassals || rank >= TitleRank.Earl;

        // Rank wins before area, so a large barony cannot suppress a duke's label.
        public static long Priority(PoliticalMapLabelTier tier, TitleRank? rank, int cells) =>
            ((long)tier << 40) | ((long)(rank is { } value ? (int)value + 1 : 0) << 32) | (uint)cells;
    }
}
