using System.Collections.Generic;
using DellarteDellaGuerra.Infrastructure.Cannon.Infra.Model;

namespace DellarteDellaGuerra.Infrastructure.SiegeEngines.Port;

public interface ICannonIconProvider
{
    IEnumerable<DeploymentSiegeEngineIcon> GetSiegeEngineIcons();
}
