using System;
using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Infrastructure.SiegeEngines.Port;

namespace DellarteDellaGuerra.Infrastructure.SiegeEngines;

public class CannonRegistry : ICannonRegistry
{
    private readonly List<Domain.SiegeEngines.Model.CannonProperties> _cannonTypes = new();
    private readonly Dictionary<string, ICannonFactory> _factories = new();

    public void RegisterCannonType(Domain.SiegeEngines.Model.CannonProperties cannonProperties, ICannonFactory factory)
    {
        _cannonTypes.Add(cannonProperties);
        _factories[cannonProperties.Id] = factory;
    }

    public Domain.SiegeEngines.Model.CannonProperties GetCannonType(string id)
    {
        return _cannonTypes.FirstOrDefault(ct => ct.Id == id);
    }

    public Domain.SiegeEngines.Model.CannonProperties GetCannonTypeByScriptType(Type scriptType)
    {
        var cannonId = _factories.FirstOrDefault(kvp => kvp.Value.CannonScriptType == scriptType).Key;
        return cannonId != null ? GetCannonType(cannonId) : null;
    }

    public ICannonFactory GetFactory(string id) =>
        _factories.TryGetValue(id, out var factory) ? factory : null;

    public IEnumerable<Domain.SiegeEngines.Model.CannonProperties> GetAllCannonTypes()
    {
        return _cannonTypes;
    }
}
