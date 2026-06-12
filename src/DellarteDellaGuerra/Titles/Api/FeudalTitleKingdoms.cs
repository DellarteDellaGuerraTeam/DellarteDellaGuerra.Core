using System.Linq;
using DellarteDellaGuerra.Domain.Titles.Model;
using TaleWorlds.CampaignSystem;

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

            string? holderClanId = (current is not null ? titles.GetTitle(current)?.HolderClanId : null)
                                   ?? titles.GetTitle(titleId)?.HolderClanId;
            return FindClan(holderClanId)?.Kingdom;
        }

        public static Clan? FindClan(string? clanId) =>
            clanId is null
                ? null
                : Clan.All.FirstOrDefault(clan => clan.StringId == clanId && !clan.IsEliminated);
    }
}