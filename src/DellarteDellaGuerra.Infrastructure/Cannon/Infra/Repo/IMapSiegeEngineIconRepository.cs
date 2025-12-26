using System.Collections.Generic;
using DellarteDellaGuerra.Cannon.UI.Model;
using DellarteDellaGuerra.Infrastructure.Cannon.UI.Model;

namespace DellarteDellaGuerra.Cannon.UI;

public interface IMapSiegeEngineIconRepository
{
    ISet<MapSiegeEngineIcon> MapSiegeEngineIcons { get; }
}