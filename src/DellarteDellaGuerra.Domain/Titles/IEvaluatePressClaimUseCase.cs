using DellarteDellaGuerra.Domain.Titles.Model;

namespace DellarteDellaGuerra.Domain.Titles
{
    public interface IEvaluatePressClaimUseCase
    {
        float Execute(ClaimOpportunity opportunity);
    }
}
