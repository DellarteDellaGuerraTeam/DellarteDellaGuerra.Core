using System.Linq;
using DellarteDellaGuerra.Domain.Titles.Model;
using DellarteDellaGuerra.Domain.Titles.Port;

namespace DellarteDellaGuerra.Domain.Titles
{
    /**
     * <summary>
     *  Whom a clan serves: the nearest holder above its primary title in the de jure hierarchy
     *  who is not the clan itself.
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

        // The title a clan serves under: its highest. On a tie it is the one held longest, so
        // gaining a title of the same rank does not change its liege; then the listing order.
        public Title? GetPrimaryTitle(string clanId)
        {
            return _titleRepository.GetTitlesByClan(clanId)
                .OrderByDescending(title => title.Rank)
                .ThenBy(title => title.HeldSinceDay)
                .FirstOrDefault();
        }
    }
}
