using System.Collections.Generic;
using DellarteDellaGuerra.Infrastructure.Cannon.UI.Model;

namespace DellarteDellaGuerra.Infrastructure.Cannon.UI.Repo;

public interface IPrefabSiegeEngineRepository
{
    ISet<SiegeEngineMapPrefab> GetPrefabSiegeEngines();
}