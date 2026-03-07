using System;
using System.Collections.Generic;

namespace DellarteDellaGuerra.Infrastructure.SiegeEngines.Port;

public interface ICannonRegistry
{
    void RegisterCannonType(Domain.SiegeEngines.Model.Cannon cannon, ICannonFactory factory);
    Domain.SiegeEngines.Model.Cannon GetCannonType(string id);
    Domain.SiegeEngines.Model.Cannon GetCannonTypeByScriptType(Type scriptType);
    ICannonFactory GetFactory(string id);
    IEnumerable<Domain.SiegeEngines.Model.Cannon> GetAllCannonTypes();
}
