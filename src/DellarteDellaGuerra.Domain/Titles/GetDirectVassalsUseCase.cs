using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.Titles.Port;

namespace DellarteDellaGuerra.Domain.Titles
{
    public class GetDirectVassalsUseCase : IGetDirectVassalsUseCase
    {
        private readonly ITitleRepository _titleRepository;
        private readonly IFeudalStructure _feudalStructure;
        private readonly SuzeraintyPolicy _suzeraintyPolicy;
        private readonly IGenealogy _genealogy;

        public GetDirectVassalsUseCase(
            ITitleRepository titleRepository,
            IFeudalStructure feudalStructure,
            SuzeraintyPolicy suzeraintyPolicy,
            IGenealogy genealogy)
        {
            _titleRepository = titleRepository;
            _feudalStructure = feudalStructure;
            _suzeraintyPolicy = suzeraintyPolicy;
            _genealogy = genealogy;
        }

        public IReadOnlyList<string> Execute(string clanId)
        {
            return _titleRepository
                .GetAllTitles()
                .Select(title => _genealogy.GetHolderClanOf(title))
                .Where(holderClanId => holderClanId != null && holderClanId != clanId)
                .Distinct()
                .Where(holderClanId => _suzeraintyPolicy.GetSuzerain(holderClanId!) == clanId)
                .Select(holderClanId => holderClanId!)
                .ToList();
        }
    }
}
