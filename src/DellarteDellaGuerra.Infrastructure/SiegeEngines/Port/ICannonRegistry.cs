using System;
using System.Collections.Generic;

namespace DellarteDellaGuerra.Infrastructure.SiegeEngines.Port;

public interface ICannonRegistry
{
    void RegisterCannonType(ICannonType cannonType, ICannonFactory factory);
    ICannonType GetCannonType(string id);
    ICannonType GetCannonTypeByScriptType(Type scriptType);
    ICannonFactory GetFactory(string id);
    IEnumerable<ICannonType> GetAllCannonTypes();
}
