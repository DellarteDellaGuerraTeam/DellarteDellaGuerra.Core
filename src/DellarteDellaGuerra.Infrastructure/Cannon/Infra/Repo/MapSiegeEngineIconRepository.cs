using System.Collections.Generic;
using DellarteDellaGuerra.Infrastructure.Cannon.Infra.Model;

namespace DellarteDellaGuerra.Infrastructure.Cannon.Infra.Repo;

public class MapSiegeEngineIconRepository : IMapSiegeEngineIconRepository
{
    public ISet<MapSiegeEngineIcon> MapSiegeEngineIcons =>
        new HashSet<MapSiegeEngineIcon>
        {
            /*Checkout MachineTypes*/
            new(8, "falconet")
        };
}