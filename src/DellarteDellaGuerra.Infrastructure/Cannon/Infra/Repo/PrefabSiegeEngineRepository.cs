using System.Collections.Generic;
using DellarteDellaGuerra.Infrastructure.Cannon.Infra.Model;

namespace DellarteDellaGuerra.Infrastructure.Cannon.Infra.Repo;

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