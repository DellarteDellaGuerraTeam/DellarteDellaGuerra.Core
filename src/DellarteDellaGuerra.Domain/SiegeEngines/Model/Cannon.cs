namespace DellarteDellaGuerra.Domain.SiegeEngines.Model
{
    public record Cannon(
        string Id,
        string DisplayName,
        string SiegeDeploymentSelectionIconSpriteId,
        string MapSiegeMarkerSpriteId,
        string CampaignMapSelectionIconSpriteId,
        string CampaignMapPrefabName,
        string CampaignMapProjectilePrefabName,
        string CampaignMapReloadAnimationName,
        string CampaignMapFireAnimationName,
        int MachineType,
        int CampaignMapProjectileBoneIndex
    );
}