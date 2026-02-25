using System.Collections.Generic;
using DellarteDellaGuerra.Infrastructure.Cannon.Infra.Model;
using DellarteDellaGuerra.Infrastructure.SiegeEngines;

namespace DellarteDellaGuerra.Infrastructure.Cannon.Infra.Repo;

public class DeploymentSiegeEngineIconRepository : IDeploymentSiegeEngineIconRepository
{
    private readonly CannonIconProvider _iconProvider;

    public DeploymentSiegeEngineIconRepository(CannonIconProvider iconProvider)
    {
        _iconProvider = iconProvider;
    }

    public ISet<DeploymentSiegeEngineIcon> SiegeEngineIcons =>
        new HashSet<DeploymentSiegeEngineIcon>(_iconProvider.GetSiegeEngineIcons());
}