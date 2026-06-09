using DellarteDellaGuerra.Domain.Titles.Model;
using DellarteDellaGuerra.Domain.Titles.Port;

namespace DellarteDellaGuerra.Domain.Titles
{
    public class AccumulateTensionUseCase : IAccumulateTensionUseCase
    {
        private readonly ITensionRepository _tensionRepository;
        private readonly ITitleRepository _titleRepository;

        public AccumulateTensionUseCase(ITensionRepository tensionRepository, ITitleRepository titleRepository)
        {
            _tensionRepository = tensionRepository;
            _titleRepository = titleRepository;
        }

        public FeudalTension Execute(string claimantClanId, Claim claim, float dailyRate)
        {
            var title = _titleRepository.GetTitle(claim.TitleId);
            if (title?.HolderClanId == claimantClanId)
            {
                _tensionRepository.ResetTension(claimantClanId, claim.TitleId);
                return new FeudalTension(claimantClanId, claim.TitleId, 0f);
            }

            float currentAmount = _tensionRepository.GetTension(claimantClanId, claim.TitleId)?.Amount ?? 0f;
            float strengthFactor = claim.Strength switch
            {
                ClaimStrength.Weak => 0.5f,
                ClaimStrength.DeJure => 1.5f,
                _ => 1.0f
            };

            var tension = new FeudalTension(claimantClanId, claim.TitleId, currentAmount + dailyRate * strengthFactor);
            _tensionRepository.SetTension(tension);
            return tension;
        }
    }
}
