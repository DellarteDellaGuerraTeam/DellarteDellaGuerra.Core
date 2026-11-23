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

        /**
         * <summary>
         *  Whether the claim is worth acting on: the title must be held, and it must not be held
         *  by the claimant himself.
         * </summary>
         * <remarks>
         *  A blood claim names its claimant, and only the holder is barred — his brothers and his
         *  passed-over sons hold claims against their own house, which is what lets a house go to
         *  war with itself. A conquest claim names no hero, so it stays the clan comparison it has
         *  always been.
         * </remarks>
         */
        public bool Execute(Claim claim)
        {
            var title = _titleRepository.GetTitle(claim.TitleId);
            if (title?.HolderHeroId is null) return false;

            return claim.ClaimantHeroId is { } claimantHeroId
                ? claimantHeroId != title.HolderHeroId
                : _genealogy.GetClanOf(title.HolderHeroId) != claim.ClaimantClanId;
        }
    }
}
