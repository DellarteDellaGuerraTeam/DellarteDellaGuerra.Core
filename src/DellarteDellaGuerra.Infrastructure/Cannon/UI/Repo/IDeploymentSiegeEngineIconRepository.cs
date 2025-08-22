using System.Collections.Generic;
using DellarteDellaGuerra.Cannon.UI.Model;

namespace DellarteDellaGuerra.Cannon.UI;

public interface IDeploymentSiegeEngineIconRepository
{
    ISet<DeploymentSiegeEngineIcon> SiegeEngineIcons { get; }
}