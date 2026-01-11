using System.Collections.Generic;
using DellarteDellaGuerra.Cannon.UI;
using DellarteDellaGuerra.Infrastructure.Cannon.UI.Model;

namespace DellarteDellaGuerra.Infrastructure.Cannon.UI.Repo;

public class MapSiegeEngineIconRepository : IMapSiegeEngineIconRepository
{
    public ISet<MapSiegeEngineIcon> MapSiegeEngineIcons =>
        new HashSet<MapSiegeEngineIcon>
        {
            /*Checkout MachineTypes*/
            new(8, "falconet")
        };
}