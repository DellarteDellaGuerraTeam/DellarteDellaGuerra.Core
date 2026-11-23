using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Titles.Model;

namespace DellarteDellaGuerra.Domain.Titles.Port
{
    public interface IClaimRepository
    {
        IReadOnlyList<Claim> GetClaimsFor(string claimantClanId);
        IReadOnlyList<Claim> GetClaimsOn(string titleId);
        void AddClaim(Claim claim);
        void RemoveClaim(string claimId);
    }
}
