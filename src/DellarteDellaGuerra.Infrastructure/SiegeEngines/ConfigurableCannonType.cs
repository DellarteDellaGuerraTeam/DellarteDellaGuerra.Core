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
    public string SiegeOrderIconSpriteId => _properties.SiegeOrderIconSpriteId;
    public string MapSiegeSpriteId => _properties.MapSiegeSpriteId;
    public string SiegeDeploymentIconSpriteId => _properties.SiegeDeploymentIconSpriteId;
    public string MapPrefabName => _properties.MapPrefabName;
    public string ProjectilePrefab => _properties.ProjectilePrefab;
    public string ReloadPrefab => _properties.ReloadPrefab;
    public string FirePrefab => _properties.FirePrefab;
    public int MachineType => _properties.MachineType;
    public int ProjectileBoneIndex => _properties.ProjectileBoneIndex;

    public SiegeEngineType GetSiegeEngineType() =>
        MBObjectManager.Instance.GetObject<SiegeEngineType>(Id);
}
