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

    public SpawnableArtilleryRangedSiegeWeapon CreateCannon()
    {
        // For now, return a basic Falconet for all types
        // This can be extended to create different cannon types based on _cannonId
        return new Falconet();
    }

    public void ConfigureSpawner(SpawnerEntityMissionHelper helper)
    {
        var cannon = helper.SpawnedEntity.GetFirstScriptInFamilyDescending<Falconet>();
        // Configuration logic
    }
}