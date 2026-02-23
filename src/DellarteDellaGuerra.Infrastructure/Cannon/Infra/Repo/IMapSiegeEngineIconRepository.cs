using System.Collections.Generic;
using DellarteDellaGuerra.Infrastructure.Cannon.Infra.Model;

namespace DellarteDellaGuerra.Infrastructure.Cannon.Infra.Repo;

public interface IMapSiegeEngineIconRepository
{
    ISet<MapSiegeEngineIcon> MapSiegeEngineIcons { get; }
}