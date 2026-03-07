using System;
using DellarteDellaGuerra.Infrastructure.SiegeEngines;
using DellarteDellaGuerra.Infrastructure.SiegeEngines.Port;
using DellarteDellaGuerra.Integration.SiegeEngines.Mission.Siege.Spawn;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Integration.SiegeEngines;

public class GenericCannonFactory : ICannonFactory
{
    private readonly string _cannonId;

    public GenericCannonFactory(string cannonId)
    {
        _cannonId = cannonId;
    }

    public Type CannonScriptType => typeof(GenericCannon);

    public SpawnableArtilleryRangedSiegeWeapon CreateCannon() => new GenericCannon();

    public void ConfigureSpawner(SpawnerEntityMissionHelper helper)
    {
        var cannon = helper.SpawnedEntity.GetFirstScriptInFamilyDescending<GenericCannon>();
        // Configuration logic
    }
}
