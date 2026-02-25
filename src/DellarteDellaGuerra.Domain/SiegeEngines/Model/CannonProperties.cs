namespace DellarteDellaGuerra.Domain.SiegeEngines.Model
{
    public record CannonProperties(
        string Id,
        string DisplayName,
        string SpriteId,
        string MapPrefabName,
        string ProjectilePrefab,
        string ReloadPrefab,
        string FirePrefab,
        int MachineType,
        int ProjectileBoneIndex
    );
}