using System;
using DellarteDellaGuerra.Infrastructure.Cannon.Mission.Siege.Spawn;
using DellarteDellaGuerra.Infrastructure.SiegeEngines.Port;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Infrastructure.SiegeEngines;

public class GenericCannonFactory : ICannonFactory
{
    private readonly string _cannonId;

    public GenericCannonFactory(string cannonId)
    {
        _cannonId = cannonId;
    }

    // All XML-configured cannons share the Falconet script type until dedicated classes are added
    public Type CannonScriptType => typeof(Falconet);

    public SpawnableArtilleryRangedSiegeWeapon CreateCannon() => new Falconet();

    public void ConfigureSpawner(SpawnerEntityMissionHelper helper)
    {
        var cannon = helper.SpawnedEntity.GetFirstScriptInFamilyDescending<Falconet>();
        // Configuration logic
    }
}
