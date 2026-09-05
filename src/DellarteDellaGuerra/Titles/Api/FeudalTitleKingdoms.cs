using DellarteDellaGuerra.Domain.Titles.Model;
using TaleWorlds.CampaignSystem;
using TaleWorlds.ObjectSystem;

namespace DellarteDellaGuerra.Titles.Api
{
    /**
     * <summary>
     *  Resolves the engine kingdom a title belongs to: the de jure crown's holder clan's
     *  kingdom (titles are dignities *of* a kingdom), falling back to the title's own de jure
     *  holder when no crown title is reachable.
     * </summary>
     */
    public static class FeudalTitleKingdoms
    {
        public static Kingdom? GetTitleKingdom(string titleId)
        {
            if (!FeudalServices.IsInitialised) return null;

            var structure = FeudalServices.Structure!;
            var titles = FeudalServices.Titles!;

            string? current = titleId;
            while (current is not null)
            {
                var rank = structure.GetRank(current);
                if (rank == TitleRank.King || rank == TitleRank.Emperor) break;
                current = structure.GetDeJureSuzerainTitleId(current);
            }

            string? holderHeroId = (current is not null ? titles.GetTitle(current)?.HolderHeroId : null)
                                   ?? titles.GetTitle(titleId)?.HolderHeroId;
            return HolderClan(holderHeroId)?.Kingdom;
        }

        private static Clan? HolderClan(string? heroId) =>
            heroId is not null && MBObjectManager.Instance?.GetObject<Hero>(heroId)?.Clan is { IsEliminated: false } clan
                ? clan
                : null;
    }
}