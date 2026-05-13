using System.Collections.Generic;
using System.Linq;
using Bannerlord.Cannons.Api;

namespace DellarteDellaGuerra.Infrastructure.SiegeEngines;

public class CannonRepository : ICannonRepository
{
    private readonly ICannonApi _cannonApi;

    public CannonRepository(ICannonApi cannonApi)
    {
        _cannonApi = cannonApi;
    }

    public ISet<Cannon> GetAllCannons()
    {
        return new HashSet<Cannon>(
            _cannonApi.GetAllCannons()
                .Select(cannon => new Cannon(cannon.Id, cannon.IsDefensiveSiegeWeapon, cannon.IsAttackerSiegeWeapon))
        );
    }
}