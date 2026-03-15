using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Objects.Siege;

namespace DellarteDellaGuerra.Integration.SiegeEngines.Mission.Siege.Spawn;

public enum Team { Attacker, Defender }

public class GenericCannonSpawner : SpawnerBase
{
    [EditorVisibleScriptComponentVariable(true)]
    public Team Team = Team.Attacker;
    
    private SpawnerEntityMissionHelper? _spawnerMissionHelper;

    protected override void OnPreInit()
    {
        base.OnPreInit();
        _spawnerMissionHelper = new SpawnerEntityMissionHelper(this);
    }

    public override void AssignParameters(SpawnerEntityMissionHelper spawnerMissionHelper)
    {
        var cannon = spawnerMissionHelper.SpawnedEntity.GetFirstScriptInFamilyDescending<GenericCannon>();
        cannon.SetSide(Team == Team.Attacker ? BattleSideEnum.Attacker : BattleSideEnum.Defender);
    }
}
