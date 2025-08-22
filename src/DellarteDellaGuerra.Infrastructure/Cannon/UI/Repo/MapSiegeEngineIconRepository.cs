using System.Collections.Generic;
using DellarteDellaGuerra.Cannon.UI.Model;

namespace DellarteDellaGuerra.Cannon.UI;

public class MapSiegeEngineIconRepository : IMapSiegeEngineIconRepository
{
    public ISet<MapSiegeEngineIcon> MapSiegeEngineIcons =>
        new HashSet<MapSiegeEngineIcon>
        {
            /*Checkout MachineTypes*/
            new(8, "falconet")
        };
}