using System.Collections.Generic;
using DellarteDellaGuerra.Infrastructure.Cannon.UI.Model;

namespace DellarteDellaGuerra.Infrastructure.Cannon.UI.Repo;

public class PrefabSiegeEngineRepository : IPrefabSiegeEngineRepository
{
    public ISet<SiegeEngineMapPrefab> GetPrefabSiegeEngines()
    {
        return new HashSet<SiegeEngineMapPrefab>
        {
            new(
                "falconet",
                "dadg_falconet_mapicon")
        };
    }
}