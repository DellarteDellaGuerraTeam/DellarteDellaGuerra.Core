using System;
using System.Collections.Generic;

namespace DellarteDellaGuerra.Infrastructure.SiegeEngines.Port;

public interface ICannonRegistry
{
    void RegisterCannonType(Domain.SiegeEngines.Model.CannonProperties cannonProperties, ICannonFactory factory);
    Domain.SiegeEngines.Model.CannonProperties GetCannonType(string id);
    Domain.SiegeEngines.Model.CannonProperties GetCannonTypeByScriptType(Type scriptType);
    ICannonFactory GetFactory(string id);
    IEnumerable<Domain.SiegeEngines.Model.CannonProperties> GetAllCannonTypes();
}
