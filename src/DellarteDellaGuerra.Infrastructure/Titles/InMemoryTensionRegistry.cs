using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.Titles.Model;
using DellarteDellaGuerra.Domain.Titles.Port;

namespace DellarteDellaGuerra.Infrastructure.Titles;

/**
 * <summary>
 * An in-memory registry of the feudal tension a claimant clan has built up against a title.
 * </summary>
 */
public class InMemoryTensionRegistry : ITensionRepository
{
    private readonly Dictionary<(string ClaimantClanId, string TitleId), FeudalTension> _tensions =
        new Dictionary<(string, string), FeudalTension>();

    /**
     * <summary>
     * Clears the registry and loads the given tensions, typically on campaign start or save load.
     * </summary>
     */
    public void Initialise(IEnumerable<FeudalTension> tensions)
    {
        _tensions.Clear();
        foreach (FeudalTension tension in tensions)
        {
            SetTension(tension);
        }
    }

    /**
     * <summary>
     * Returns a copy of all the registered tensions, typically for save game serialisation.
     * </summary>
     */
    public IReadOnlyList<FeudalTension> Snapshot()
    {
        return _tensions.Values.ToList();
    }

    public FeudalTension? GetTension(string claimantClanId, string titleId)
    {
        return _tensions.TryGetValue((claimantClanId, titleId), out FeudalTension tension) ? tension : null;
    }

    public IReadOnlyList<FeudalTension> GetTensionsFor(string claimantClanId)
    {
        return _tensions.Values.Where(tension => tension.ClaimantClanId == claimantClanId).ToList();
    }

    public void SetTension(FeudalTension tension)
    {
        _tensions[(tension.ClaimantClanId, tension.TitleId)] = tension;
    }

    public void ResetTension(string claimantClanId, string titleId)
    {
        _tensions.Remove((claimantClanId, titleId));
    }
}
