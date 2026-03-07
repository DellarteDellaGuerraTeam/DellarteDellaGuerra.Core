using System;
using System.Collections.Generic;

namespace DellarteDellaGuerra.Infrastructure.SiegeEngines.Port;

public interface ICannonRegistry
{
    void RegisterCannon(Domain.SiegeEngines.Model.Cannon cannon, ICannonFactory factory);
    Domain.SiegeEngines.Model.Cannon GetCannon(string id);
    Domain.SiegeEngines.Model.Cannon GetCannonByScript(Type scriptType);
    ICannonFactory GetFactory(string id);
    IEnumerable<Domain.SiegeEngines.Model.Cannon> GetAllCannons();
}
