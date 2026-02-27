using System;
using DellarteDellaGuerra.Infrastructure.Cannon.Mission.Siege.Spawn;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Infrastructure.SiegeEngines.Port;

public interface ICannonFactory
{
    Type CannonScriptType { get; }
    SpawnableArtilleryRangedSiegeWeapon CreateCannon();
    void ConfigureSpawner(SpawnerEntityMissionHelper helper);
}