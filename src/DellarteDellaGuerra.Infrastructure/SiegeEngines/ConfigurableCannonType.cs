using DellarteDellaGuerra.Domain.SiegeEngines.Model;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using ICannonType = DellarteDellaGuerra.Infrastructure.SiegeEngines.Port.ICannonType;

namespace DellarteDellaGuerra.Infrastructure.SiegeEngines;

public class ConfigurableCannonType : ICannonType
{
    private readonly CannonProperties _properties;

    public ConfigurableCannonType(CannonProperties properties)
    {
        _properties = properties;
    }

    public string Id => _properties.Id;
    public string DisplayName => _properties.DisplayName;
    public string SpriteId => _properties.SpriteId;
    public string MapPrefabName => _properties.MapPrefabName;
    public string ProjectilePrefab => _properties.ProjectilePrefab;
    public string ReloadPrefab => _properties.ReloadPrefab;
    public string FirePrefab => _properties.FirePrefab;
    public int MachineType => _properties.MachineType;
    public int ProjectileBoneIndex => _properties.ProjectileBoneIndex;

    public SiegeEngineType GetSiegeEngineType()
    {
        return MBObjectManager.Instance.GetObject<SiegeEngineType>(Id);
    }
}