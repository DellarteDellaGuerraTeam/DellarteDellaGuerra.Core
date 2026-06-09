using System.Linq;
using DellarteDellaGuerra.Domain.Titles.Model;
using DellarteDellaGuerra.Domain.Titles.Port;

namespace DellarteDellaGuerra.Domain.Titles
{
    public class ComputeInfluenceTierBonusUseCase : IComputeInfluenceTierBonusUseCase
    {
        private readonly ITitleRepository _titleRepository;

        public ComputeInfluenceTierBonusUseCase(ITitleRepository titleRepository)
        {
            _titleRepository = titleRepository;
        }

        public float Execute(string clanId)
        {
            var titles = _titleRepository.GetTitlesByClan(clanId);
            if (titles.Count == 0) return 0f;

            var highestRank = titles.Max(title => title.Rank);
            return highestRank switch
            {
                TitleRank.Baron => 0.5f,
                TitleRank.Count => 1.0f,
                TitleRank.Duke => 2.0f,
                TitleRank.King => 3.0f,
                TitleRank.Emperor => 4.0f,
                _ => 0f
            };
        }
    }
}
