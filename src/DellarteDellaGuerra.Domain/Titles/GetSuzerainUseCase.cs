using System.Linq;
using DellarteDellaGuerra.Domain.Titles.Model;
using DellarteDellaGuerra.Domain.Titles.Port;

namespace DellarteDellaGuerra.Domain.Titles
{
    public class GetSuzerainUseCase : IGetSuzerainUseCase
    {
        private readonly ITitleRepository _titleRepository;
        private readonly IFeudalStructure _feudalStructure;

        public GetSuzerainUseCase(ITitleRepository titleRepository, IFeudalStructure feudalStructure)
        {
            _titleRepository = titleRepository;
            _feudalStructure = feudalStructure;
        }

        public string? Execute(string clanId)
        {
            var highestTitle = GetHighestTitle(clanId);
            if (highestTitle is null) return null;

            string? parentTitleId = _feudalStructure.GetDeJureSuzerainTitleId(highestTitle.Id);
            while (parentTitleId != null)
            {
                string? holderClanId = _titleRepository.GetTitle(parentTitleId)?.HolderClanId;
                if (holderClanId != null && holderClanId != clanId)
                {
                    return holderClanId;
                }

                parentTitleId = _feudalStructure.GetDeJureSuzerainTitleId(parentTitleId);
            }

            return null;
        }

        public TitleRank? GetHighestRank(string clanId) => GetHighestTitle(clanId)?.Rank;

        private Title? GetHighestTitle(string clanId)
        {
            return _titleRepository
                .GetTitlesByClan(clanId)
                .OrderByDescending(title => title.Rank)
                .FirstOrDefault();
        }
    }
}
