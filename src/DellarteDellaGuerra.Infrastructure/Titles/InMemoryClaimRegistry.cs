using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.Titles.Model;
using DellarteDellaGuerra.Domain.Titles.Port;

namespace DellarteDellaGuerra.Infrastructure.Titles;

/**
 * <summary>
 * An in-memory registry of the claims clans hold on feudal titles.
 * </summary>
 */
public class InMemoryClaimRegistry : IClaimRepository
{
    private readonly Dictionary<string, Claim> _claimsByClaimId = new Dictionary<string, Claim>();

    /**
     * <summary>
     * Clears the registry and loads the given claims, typically on campaign start or save load.
     * </summary>
     */
    public void Initialise(IEnumerable<Claim> claims)
    {
        _claimsByClaimId.Clear();
        foreach (Claim claim in claims)
        {
            AddClaim(claim);
        }
    }

    /**
     * <summary>
     * Returns a copy of all the registered claims, typically for save game serialisation.
     * </summary>
     */
    public IReadOnlyList<Claim> Snapshot()
    {
        return _claimsByClaimId.Values.ToList();
    }

    public IReadOnlyList<Claim> GetClaimsFor(string claimantClanId)
    {
        return _claimsByClaimId.Values.Where(claim => claim.ClaimantClanId == claimantClanId).ToList();
    }

    public IReadOnlyList<Claim> GetClaimsOn(string titleId)
    {
        return _claimsByClaimId.Values.Where(claim => claim.TitleId == titleId).ToList();
    }

    public void AddClaim(Claim claim)
    {
        _claimsByClaimId[claim.Id] = claim;
    }

    public void RemoveClaim(string claimId)
    {
        _claimsByClaimId.Remove(claimId);
    }
}
