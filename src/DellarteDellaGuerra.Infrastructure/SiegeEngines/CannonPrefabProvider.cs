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

    public string GetCampaignMapPrefabName(string cannonId, int wallLevel, BattleSideEnum side)
    {
        return _cannonRegistry.GetCannonType(cannonId)?.CampaignMapPrefabName;
    }

    public string GetCampaignMapProjectilePrefabName(string cannonId)
    {
        return _cannonRegistry.GetCannonType(cannonId)?.CampaignMapProjectilePrefabName;
    }

    public string GetCampaignMapReloadAnimationName(string cannonId)
    {
        return _cannonRegistry.GetCannonType(cannonId)?.CampaignMapReloadAnimationName;
    }

    public string GetCampaignMapFireAnimationName(string cannonId)
    {
        return _cannonRegistry.GetCannonType(cannonId)?.CampaignMapFireAnimationName;
    }

    public int GetCampaignMapProjectileBoneIndex(string cannonId)
    {
        return _cannonRegistry.GetCannonType(cannonId)?.CampaignMapProjectileBoneIndex ?? -1;
    }
}
