using DellarteDellaGuerra.Domain.Titles.Model;
using DellarteDellaGuerra.Domain.Titles.Port;

namespace DellarteDellaGuerra.Domain.Titles
{
    public class EvaluateClaimUseCase : IEvaluateClaimUseCase
    {
        private readonly ITitleRepository _titleRepository;

        public EvaluateClaimUseCase(ITitleRepository titleRepository)
        {
            _titleRepository = titleRepository;
        }

        public bool Execute(Claim claim)
        {
            var title = _titleRepository.GetTitle(claim.TitleId);
            return title != null
                   && title.HolderClanId != null
                   && title.HolderClanId != claim.ClaimantClanId;
        }
    }
}
