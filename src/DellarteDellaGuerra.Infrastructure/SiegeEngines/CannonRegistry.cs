using System;
using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Infrastructure.SiegeEngines.Port;

namespace DellarteDellaGuerra.Infrastructure.SiegeEngines;

public class CannonRegistry : ICannonRegistry
{
    private readonly List<ICannonType> _cannonTypes = new();
    private readonly Dictionary<string, ICannonFactory> _factories = new();

    public void RegisterCannonType(ICannonType cannonType, ICannonFactory factory)
    {
        _cannonTypes.Add(cannonType);
        _factories[cannonType.Id] = factory;
    }

    public ICannonType GetCannonType(string id) =>
        _cannonTypes.FirstOrDefault(ct => ct.Id == id);

    public ICannonType GetCannonTypeByScriptType(Type scriptType)
    {
        var cannonId = _factories.FirstOrDefault(kvp => kvp.Value.CannonScriptType == scriptType).Key;
        return cannonId != null ? GetCannonType(cannonId) : null;
    }

    public ICannonFactory GetFactory(string id) =>
        _factories.TryGetValue(id, out var factory) ? factory : null;

    public IEnumerable<ICannonType> GetAllCannonTypes() => _cannonTypes;
}
