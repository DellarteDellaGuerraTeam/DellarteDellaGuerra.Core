using DellarteDellaGuerra.Domain.PoliticalMap.Overlay;
using DellarteDellaGuerra.Domain.Titles.Model;

namespace DellarteDellaGuerra.Domain.PoliticalMap.Labels
{
    public static class PoliticalMapLabelHierarchy
    {
        public static int SizeIndexFor(PoliticalMapLabelTier tier, TitleRank? rank, float pixelRadius)
        {
            int size = PoliticalMapLabelLayout.SizeIndexFor(pixelRadius);
            if (size < 0 || tier != PoliticalMapLabelTier.Territory) return size;
            return rank switch
            {
                >= TitleRank.Duke => 6,
                TitleRank.Earl => System.Math.Min(System.Math.Max(size, 4), 5),
                _ => System.Math.Min(size, 2)
            };
        }

        public static bool Shows(PoliticalMapLabelTier tier, TitleRank? rank, PoliticalMapOwnerLevel level) =>
            tier != PoliticalMapLabelTier.Owner || level == PoliticalMapOwnerLevel.SubVassals || rank >= TitleRank.Earl;

        // Rank wins before area, so a large barony cannot suppress a duke's label.
        public static long Priority(PoliticalMapLabelTier tier, TitleRank? rank, int cells) =>
            ((long)tier << 40) | ((long)(rank is { } value ? (int)value + 1 : 0) << 32) | (uint)cells;
    }
}
