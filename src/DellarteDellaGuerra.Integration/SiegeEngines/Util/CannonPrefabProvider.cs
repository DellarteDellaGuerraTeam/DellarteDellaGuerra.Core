using DellarteDellaGuerra.Infrastructure.SiegeEngines.Port;
using DellarteDellaGuerra.Integration.SiegeEngines.Port;
using TaleWorlds.Core;

namespace DellarteDellaGuerra.Integration.SiegeEngines;

public class CannonPrefabProvider : ICannonPrefabProvider
{
    private readonly ICannonRegistry _cannonRegistry;

    public CannonPrefabProvider(ICannonRegistry cannonRegistry)
    {
        _cannonRegistry = cannonRegistry;
    }

    public string GetCampaignMapPrefabName(string cannonId, int wallLevel, BattleSideEnum side)
    {
        return _cannonRegistry.GetCannon(cannonId)?.CampaignMapPrefabName;
    }

    public string GetCampaignMapProjectilePrefabName(string cannonId)
    {
        return _cannonRegistry.GetCannon(cannonId)?.CampaignMapProjectilePrefabName;
    }

    public string GetCampaignMapReloadAnimationName(string cannonId)
    {
        return _cannonRegistry.GetCannon(cannonId)?.CampaignMapReloadAnimationName;
    }

    public string GetCampaignMapFireAnimationName(string cannonId)
    {
        return _cannonRegistry.GetCannon(cannonId)?.CampaignMapFireAnimationName;
    }

    public int GetCampaignMapProjectileBoneIndex(string cannonId)
    {
        return _cannonRegistry.GetCannon(cannonId)?.CampaignMapProjectileBoneIndex ?? -1;
    }
}
