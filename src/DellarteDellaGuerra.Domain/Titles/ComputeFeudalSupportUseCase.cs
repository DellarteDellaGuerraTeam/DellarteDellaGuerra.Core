using System.Linq;
using DellarteDellaGuerra.Domain.Titles.Port;

namespace DellarteDellaGuerra.Domain.Titles
{
    public class ComputeFeudalSupportUseCase : IComputeFeudalSupportUseCase
    {
        private const float SuzerainWeight = 3.0f;
        private const float PeerWeight = 1.5f;
        private const float OutsiderWeight = 0.2f;
        private const float NeutralWeight = 1.0f;

        private readonly ITitleRepository _titleRepository;
        private readonly IFeudalStructure _feudalStructure;
        private readonly IGetSuzerainUseCase _getSuzerainUseCase;

        public ComputeFeudalSupportUseCase(
            ITitleRepository titleRepository,
            IFeudalStructure feudalStructure,
            IGetSuzerainUseCase getSuzerainUseCase)
        {
            _titleRepository = titleRepository;
            _feudalStructure = feudalStructure;
            _getSuzerainUseCase = getSuzerainUseCase;
        }

        public float Execute(string voterClanId, string settlementId)
        {
            string? settlementTitleId = _feudalStructure.GetTitleIdBySeat(settlementId);
            if (settlementTitleId is null) return NeutralWeight;

            string? suzerainTitleId = _feudalStructure.GetDeJureSuzerainTitleId(settlementTitleId);
            if (suzerainTitleId is null) return OutsiderWeight;

            if (_titleRepository.GetTitle(suzerainTitleId)?.HolderClanId == voterClanId)
            {
                return SuzerainWeight;
            }

            bool isPeer = _titleRepository
                .GetTitlesByClan(voterClanId)
                .Any(title => _feudalStructure.GetDeJureSuzerainTitleId(title.Id) == suzerainTitleId);

            return isPeer ? PeerWeight : OutsiderWeight;
        }
    }
}
