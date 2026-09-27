using System.Linq;
using DellarteDellaGuerra.Domain.Titles.Model;
using DellarteDellaGuerra.Domain.Titles.Port;

namespace DellarteDellaGuerra.Domain.Titles
{
    public class GetSuzerainUseCase : IGetSuzerainUseCase
    {
        private readonly ITitleRepository _titleRepository;
        private readonly IFeudalStructure _feudalStructure;
        private readonly IGenealogy _genealogy;

        public GetSuzerainUseCase(ITitleRepository titleRepository, IFeudalStructure feudalStructure, IGenealogy genealogy)
        {
            _titleRepository = titleRepository;
            _feudalStructure = feudalStructure;
            _genealogy = genealogy;
        }

        public string? Execute(string clanId)
        {
            var primaryTitle = GetPrimaryTitle(clanId);
            if (primaryTitle is null) return null;

            string? parentTitleId = _feudalStructure.GetDeJureSuzerainTitleId(primaryTitle.Id);
            while (parentTitleId != null)
            {
                string? holderClanId = _genealogy.GetHolderClanOf(_titleRepository.GetTitle(parentTitleId));
                if (holderClanId != null && holderClanId != clanId)
                {
                    return holderClanId;
                }

                parentTitleId = _feudalStructure.GetDeJureSuzerainTitleId(parentTitleId);
            }

            return null;
        }

        public TitleRank? GetHighestRank(string clanId) => GetPrimaryTitle(clanId)?.Rank;

        // The title a clan serves under: its highest. On a tie it keeps the one it already
        // served under, so gaining a title of the same rank does not change its liege.
        public Title? GetPrimaryTitle(string clanId)
        {
            var heldTitles = _titleRepository.GetTitlesByClan(clanId);
            Title? highestTitle = heldTitles.OrderByDescending(title => title.Rank).FirstOrDefault();
            if (highestTitle is null) return null;

            string? pinnedTitleId = _titleRepository.GetPrimaryTitleId(clanId);
            Title? pinnedTitle = heldTitles.FirstOrDefault(title => title.Id == pinnedTitleId);
            if (pinnedTitle is not null && pinnedTitle.Rank >= highestTitle.Rank) return pinnedTitle;

            _titleRepository.SavePrimaryTitleId(clanId, highestTitle.Id);
            return highestTitle;
        }
    }
}
