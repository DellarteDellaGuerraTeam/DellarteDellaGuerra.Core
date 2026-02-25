using DellarteDellaGuerra.Infrastructure.Cannon.Mission.Siege.Spawn;
using DellarteDellaGuerra.Infrastructure.SiegeEngines.Port;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Infrastructure.SiegeEngines;

public class FalconetFactory : ICannonFactory
{
    public SpawnableArtilleryRangedSiegeWeapon CreateCannon()
    {
        return new Falconet();
    }

    public void ConfigureSpawner(SpawnerEntityMissionHelper helper)
    {
        var cannon = helper.SpawnedEntity.GetFirstScriptInFamilyDescending<Falconet>();
        // Configuration logic
    }
}