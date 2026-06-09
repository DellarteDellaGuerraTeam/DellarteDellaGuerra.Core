using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Titles.Model;

namespace DellarteDellaGuerra.Domain.Titles.Port
{
    public interface ITensionRepository
    {
        FeudalTension? GetTension(string claimantClanId, string titleId);
        IReadOnlyList<FeudalTension> GetTensionsFor(string claimantClanId);
        void SetTension(FeudalTension tension);
        void ResetTension(string claimantClanId, string titleId);
    }
}
