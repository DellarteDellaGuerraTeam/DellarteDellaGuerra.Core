using DellarteDellaGuerra.Domain.SiegeEngines;
using TaleWorlds.Core;

namespace DellarteDellaGuerra.Infrastructure.SiegeEngines;

public class CannonPrefabProvider
{
    private readonly CannonRegistry _cannonRegistry;

    public CannonPrefabProvider(CannonRegistry cannonRegistry)
    {
        _cannonRegistry = cannonRegistry;
    }

    public string GetMapPrefabName(string cannonId, int wallLevel, BattleSideEnum side)
    {
        var cannonType = _cannonRegistry.GetCannonType(cannonId);
        return cannonType?.MapPrefabName;
    }

    public string GetProjectilePrefabName(string cannonId)
    {
        var cannonType = _cannonRegistry.GetCannonType(cannonId);
        return cannonType?.ProjectilePrefab;
    }

    public string GetReloadPrefabName(string cannonId)
    {
        var cannonType = _cannonRegistry.GetCannonType(cannonId);
        return cannonType?.ReloadPrefab;
    }

    public string GetFirePrefabName(string cannonId)
    {
        var cannonType = _cannonRegistry.GetCannonType(cannonId);
        return cannonType?.FirePrefab;
    }

    public int GetProjectileBoneIndex(string cannonId)
    {
        var cannonType = _cannonRegistry.GetCannonType(cannonId);
        return cannonType?.ProjectileBoneIndex ?? -1;
    }
}