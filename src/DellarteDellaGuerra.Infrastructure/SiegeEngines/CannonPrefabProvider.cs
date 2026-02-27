using DellarteDellaGuerra.Infrastructure.SiegeEngines.Port;
using TaleWorlds.Core;

namespace DellarteDellaGuerra.Infrastructure.SiegeEngines;

public class CannonPrefabProvider : ICannonPrefabProvider
{
    private readonly ICannonRegistry _cannonRegistry;

    public CannonPrefabProvider(ICannonRegistry cannonRegistry)
    {
        _cannonRegistry = cannonRegistry;
    }

    public string GetMapPrefabName(string cannonId, int wallLevel, BattleSideEnum side) =>
        _cannonRegistry.GetCannonType(cannonId)?.MapPrefabName;

    public string GetProjectilePrefabName(string cannonId) =>
        _cannonRegistry.GetCannonType(cannonId)?.ProjectilePrefab;

    public string GetReloadPrefabName(string cannonId) =>
        _cannonRegistry.GetCannonType(cannonId)?.ReloadPrefab;

    public string GetFirePrefabName(string cannonId) =>
        _cannonRegistry.GetCannonType(cannonId)?.FirePrefab;

    public int GetProjectileBoneIndex(string cannonId) =>
        _cannonRegistry.GetCannonType(cannonId)?.ProjectileBoneIndex ?? -1;
}
