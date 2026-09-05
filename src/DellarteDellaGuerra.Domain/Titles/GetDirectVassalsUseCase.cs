using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.Titles.Port;

namespace DellarteDellaGuerra.Domain.Titles
{
    public class GetDirectVassalsUseCase : IGetDirectVassalsUseCase
    {
        private readonly ITitleRepository _titleRepository;
        private readonly IFeudalStructure _feudalStructure;
        private readonly IGetSuzerainUseCase _getSuzerainUseCase;
        private readonly IGenealogy _genealogy;

        public GetDirectVassalsUseCase(
            ITitleRepository titleRepository,
            IFeudalStructure feudalStructure,
            IGetSuzerainUseCase getSuzerainUseCase,
            IGenealogy genealogy)
        {
            _titleRepository = titleRepository;
            _feudalStructure = feudalStructure;
            _getSuzerainUseCase = getSuzerainUseCase;
            _genealogy = genealogy;
        }

        public IReadOnlyList<string> Execute(string clanId)
        {
            return _titleRepository
                .GetAllTitles()
                .Select(title => _genealogy.GetHolderClanOf(title))
                .Where(holderClanId => holderClanId != null && holderClanId != clanId)
                .Distinct()
                .Where(holderClanId => _getSuzerainUseCase.Execute(holderClanId!) == clanId)
                .Select(holderClanId => holderClanId!)
                .ToList();
        }
    }
}
