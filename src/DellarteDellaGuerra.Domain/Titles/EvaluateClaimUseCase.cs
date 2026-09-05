using DellarteDellaGuerra.Domain.Titles.Model;
using DellarteDellaGuerra.Domain.Titles.Port;

namespace DellarteDellaGuerra.Domain.Titles
{
    public class EvaluateClaimUseCase : IEvaluateClaimUseCase
    {
        private readonly ITitleRepository _titleRepository;
        private readonly IGenealogy _genealogy;

        public EvaluateClaimUseCase(ITitleRepository titleRepository, IGenealogy genealogy)
        {
            _titleRepository = titleRepository;
            _genealogy = genealogy;
        }

        public bool Execute(Claim claim)
        {
            string? holderClanId = _genealogy.GetHolderClanOf(_titleRepository.GetTitle(claim.TitleId));
            return holderClanId != null && holderClanId != claim.ClaimantClanId;
        }
    }
}
