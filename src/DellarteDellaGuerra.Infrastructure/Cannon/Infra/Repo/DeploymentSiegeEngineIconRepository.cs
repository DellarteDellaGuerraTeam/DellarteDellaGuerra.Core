using System.Collections.Generic;
using DellarteDellaGuerra.Infrastructure.Cannon.Infra.Model;

namespace DellarteDellaGuerra.Infrastructure.Cannon.Infra.Repo;

public class DeploymentSiegeEngineIconRepository : IDeploymentSiegeEngineIconRepository
{
    private static readonly ISet<DeploymentSiegeEngineIcon> _siegeEngineIcons = new HashSet<DeploymentSiegeEngineIcon>
    {
        new("Falconet", "falconet", 8 /*Checkout MachineTypes*/)
    };

    public ISet<DeploymentSiegeEngineIcon> SiegeEngineIcons => _siegeEngineIcons;
}