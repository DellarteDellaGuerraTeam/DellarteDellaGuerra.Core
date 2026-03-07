using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Infrastructure.Cannon.Infra.Model;
using DellarteDellaGuerra.Infrastructure.SiegeEngines.Port;

namespace DellarteDellaGuerra.Infrastructure.Cannon.Infra.Repo;

public class MapSiegeEngineIconRepository : IMapSiegeEngineIconRepository
{
    private readonly ICannonRegistry _cannonRegistry;

    public MapSiegeEngineIconRepository(ICannonRegistry cannonRegistry)
    {
        _cannonRegistry = cannonRegistry;
    }

    public ISet<MapSiegeEngineIcon> MapSiegeEngineIcons =>
        new HashSet<MapSiegeEngineIcon>(
            _cannonRegistry.GetAllCannonTypes()
                .Select(ct => new MapSiegeEngineIcon(ct.MachineType, ct.Id, ct.MapSiegeMarkerSpriteId)));
}
