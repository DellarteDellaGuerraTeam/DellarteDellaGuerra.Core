using TaleWorlds.Core;

namespace DellarteDellaGuerra.Integration.SiegeEngines.Port
{
    public interface ICannonPrefabProvider
    {
        string GetCampaignMapPrefabName(string cannonId, int wallLevel, BattleSideEnum side);
        string GetCampaignMapProjectilePrefabName(string cannonId);
        string GetCampaignMapReloadAnimationName(string cannonId);
        string GetCampaignMapFireAnimationName(string cannonId);
        int GetCampaignMapProjectileBoneIndex(string cannonId);
    }
}