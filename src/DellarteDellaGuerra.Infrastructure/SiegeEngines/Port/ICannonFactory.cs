using DellarteDellaGuerra.Infrastructure.Cannon.Mission.Siege.Spawn;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Infrastructure.SiegeEngines.Port;

public interface ICannonFactory
{
    SpawnableArtilleryRangedSiegeWeapon CreateCannon();
    void ConfigureSpawner(SpawnerEntityMissionHelper helper);
}