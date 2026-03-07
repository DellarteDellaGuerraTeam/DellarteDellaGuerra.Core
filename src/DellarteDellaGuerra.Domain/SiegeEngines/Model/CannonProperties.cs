namespace DellarteDellaGuerra.Domain.SiegeEngines.Model
{
    public record CannonProperties(
        string Id,
        string DisplayName,
        string SiegeDeploymentSelectionIconSpriteId,
        string MapSiegeMarkerSpriteId,
        string CampaignMapSelectionIconSpriteId,
        string MapPrefabName,
        string ProjectilePrefab,
        string ReloadPrefab,
        string FirePrefab,
        int MachineType,
        int ProjectileBoneIndex
    );
}