using System.Collections.Generic;
using DellarteDellaGuerra.Infrastructure.Cannon.Infra.Model;
using DellarteDellaGuerra.Infrastructure.SiegeEngines.Port;

namespace DellarteDellaGuerra.Infrastructure.Cannon.Infra.Repo;

public class DeploymentSiegeEngineIconRepository : IDeploymentSiegeEngineIconRepository
{
    private readonly ICannonIconProvider _iconProvider;

    public DeploymentSiegeEngineIconRepository(ICannonIconProvider iconProvider)
    {
        _iconProvider = iconProvider;
    }

    public ISet<DeploymentSiegeEngineIcon> SiegeEngineIcons =>
        new HashSet<DeploymentSiegeEngineIcon>(_iconProvider.GetSiegeEngineIcons());
}
