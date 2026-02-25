namespace DellarteDellaGuerra.Domain.SiegeEngines.Model
{
    public interface ICannonType
    {
        string Id { get; }
        string DisplayName { get; }
        string SpriteId { get; }
        string MapPrefabName { get; }
        string ProjectilePrefab { get; }
        string ReloadPrefab { get; }
        string FirePrefab { get; }
        int MachineType { get; }
        int ProjectileBoneIndex { get; }
    }
}