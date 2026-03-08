using System;
using DellarteDellaGuerra.Infrastructure.SiegeEngines;
using DellarteDellaGuerra.Infrastructure.SiegeEngines.Port;
using DellarteDellaGuerra.Integration.SiegeEngines.Mission.Siege.Spawn;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Integration.SiegeEngines;

public class GenericCannonFactory : ICannonFactory
{
    private readonly string _cannonId;
    private readonly Type _scriptType;

    public GenericCannonFactory(string cannonId, Type scriptType)
    {
        _cannonId = cannonId;
        _scriptType = scriptType;
    }

    public Type CannonScriptType => _scriptType;

    public SpawnableArtilleryRangedSiegeWeapon CreateCannon() => (GenericCannon)Activator.CreateInstance(_scriptType)!;

    public void ConfigureSpawner(SpawnerEntityMissionHelper helper)
    {
        var cannon = helper.SpawnedEntity.GetFirstScriptInFamilyDescending<GenericCannon>();
        // Configuration logic
    }
}
