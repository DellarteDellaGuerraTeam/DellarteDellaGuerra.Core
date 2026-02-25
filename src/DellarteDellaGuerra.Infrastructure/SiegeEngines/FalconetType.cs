using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using ICannonType = DellarteDellaGuerra.Infrastructure.SiegeEngines.Port.ICannonType;

namespace DellarteDellaGuerra.Infrastructure.SiegeEngines;

public class FalconetType : ICannonType, Domain.SiegeEngines.Model.ICannonType
{
    public string Id => "falconet";
    public string DisplayName => "Falconet";
    public string SpriteId => "falconet";
    public string MapPrefabName => "dadg_falconet_mapicon";
    public string ProjectilePrefab => "cannonball_mapicon_projectile";
    public string ReloadPrefab => "ballista_a_mapicon_reload";
    public string FirePrefab => "ballista_a_mapicon_fire";
    public int MachineType => 8;
    public int ProjectileBoneIndex => 0;

    public SiegeEngineType GetSiegeEngineType()
    {
        return MBObjectManager.Instance.GetObject<SiegeEngineType>(Id);
    }
}