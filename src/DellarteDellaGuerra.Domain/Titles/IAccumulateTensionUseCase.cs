using DellarteDellaGuerra.Domain.Titles.Model;

namespace DellarteDellaGuerra.Domain.Titles
{
    public interface IAccumulateTensionUseCase
    {
        FeudalTension Execute(string claimantClanId, Claim claim, float dailyRate);
    }
}
