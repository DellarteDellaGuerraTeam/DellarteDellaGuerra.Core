using System.Collections.Generic;
using DellarteDellaGuerra.Cannon.UI.Model;

namespace DellarteDellaGuerra.Cannon.UI;

public class DeploymentSiegeEngineIconRepository : IDeploymentSiegeEngineIconRepository
{
    private static readonly ISet<DeploymentSiegeEngineIcon> _siegeEngineIcons = new HashSet<DeploymentSiegeEngineIcon>
    {
        new("Falconet", "falconet", 8 /*Checkout MachineTypes*/)
    };

    public ISet<DeploymentSiegeEngineIcon> SiegeEngineIcons => _siegeEngineIcons;
}