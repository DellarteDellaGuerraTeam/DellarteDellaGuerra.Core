using TaleWorlds.Core;

namespace DellarteDellaGuerra.Infrastructure.SiegeEngines.Port;

public interface ICannonPrefabProvider
{
    string GetMapPrefabName(string cannonId, int wallLevel, BattleSideEnum side);
    string GetProjectilePrefabName(string cannonId);
    string GetReloadPrefabName(string cannonId);
    string GetFirePrefabName(string cannonId);
    int GetProjectileBoneIndex(string cannonId);
}
