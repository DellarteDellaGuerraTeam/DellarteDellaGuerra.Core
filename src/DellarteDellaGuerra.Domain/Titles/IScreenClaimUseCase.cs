using DellarteDellaGuerra.Domain.Titles.Model;

namespace DellarteDellaGuerra.Domain.Titles
{
    public interface IScreenClaimUseCase
    {
        ClaimRejection? Execute(ClaimCandidacy candidacy);
    }
}
