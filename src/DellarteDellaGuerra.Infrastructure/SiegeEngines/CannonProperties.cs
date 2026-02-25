namespace DellarteDellaGuerra.Infrastructure.SiegeEngines;

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