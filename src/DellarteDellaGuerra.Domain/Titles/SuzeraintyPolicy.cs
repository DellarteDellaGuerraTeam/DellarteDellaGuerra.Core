using System;
using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.Titles.Model;
using DellarteDellaGuerra.Domain.Titles.Port;

namespace DellarteDellaGuerra.Domain.Titles
{
    /**
     * <summary>
     *  Whom a clan serves: the nearest higher-ranking house along its primary title's
     *  configured allegiance, falling back to territorial nesting.
     * </summary>
     */
    public class SuzeraintyPolicy
    {
        private readonly ITitleRepository _titleRepository;
        private readonly IFeudalStructure _feudalStructure;
        private readonly IGenealogy _genealogy;

        public SuzeraintyPolicy(ITitleRepository titleRepository, IFeudalStructure feudalStructure, IGenealogy genealogy)
        {
            _titleRepository = titleRepository;
            _feudalStructure = feudalStructure;
            _genealogy = genealogy;
        }

        public string? GetSuzerain(string clanId)
        {
            var primaryTitle = GetPrimaryTitle(clanId);
            if (primaryTitle is null) return null;

            var seen = new HashSet<string>(StringComparer.Ordinal) { primaryTitle.Id };
            string? parentTitleId = _feudalStructure.GetAllegianceSuzerainTitleId(primaryTitle.Id);
            while (parentTitleId != null && seen.Add(parentTitleId))
            {
                string? holderClanId = _genealogy.GetHolderClanOf(_titleRepository.GetTitle(parentTitleId));
                if (holderClanId != null && holderClanId != clanId
                    && GetPrimaryTitle(holderClanId) is { } liegeTitle && liegeTitle.Rank > primaryTitle.Rank)
                {
                    return holderClanId;
                }

                parentTitleId = _feudalStructure.GetAllegianceSuzerainTitleId(parentTitleId);
            }

            return null;
        }

        // The title a clan serves under: its highest. On a tie it is the one held longest, so
        // gaining a title of the same rank does not change its liege; then a stable title id.
        public Title? GetPrimaryTitle(string clanId)
        {
            return _titleRepository.GetTitlesByClan(clanId)
                .OrderByDescending(title => title.Rank)
                .ThenBy(title => title.HeldSinceDay)
                .ThenBy(title => title.Id, StringComparer.Ordinal)
                .FirstOrDefault();
        }
    }
}
