using System.Collections.Generic;
using DellarteDellaGuerra.Cannon.UI.Model;

namespace DellarteDellaGuerra.Infrastructure.Cannon.UI.Repo;

public interface IDeploymentSiegeEngineIconRepository
{
    ISet<DeploymentSiegeEngineIcon> SiegeEngineIcons { get; }
}