using DellarteDellaGuerra.Domain.Titles.Model;

namespace DellarteDellaGuerra.Domain.Titles
{
    public interface IEvaluateClaimUseCase
    {
        bool Execute(Claim claim);
    }
}
