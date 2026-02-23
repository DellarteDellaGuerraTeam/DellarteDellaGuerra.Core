using System.Collections.Generic;
using DellarteDellaGuerra.Infrastructure.Cannon.Infra.Model;

namespace DellarteDellaGuerra.Infrastructure.Cannon.Infra.Repo;

public interface IDeploymentSiegeEngineIconRepository
{
    ISet<DeploymentSiegeEngineIcon> SiegeEngineIcons { get; }
}