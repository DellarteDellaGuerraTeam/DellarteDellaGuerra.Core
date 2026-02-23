using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Objects.Siege;

namespace DellarteDellaGuerra.Infrastructure.Cannon.Mission.Siege.Spawn
{
    public enum Team
    {
        Attacker,
        Defender
    }
    
    public class FalconetSpawner : SpawnerBase
    {
        // [EditorVisibleScriptComponentVariable(true)]
        // public string AddOnDeployTag = "";
        //
        // [EditorVisibleScriptComponentVariable(true)]
        // public string RemoveOnDeployTag = "";
        //
        // [EditorVisibleScriptComponentVariable(true)]
        // public float DirectionRestrictionDegree = 90f;

        [EditorVisibleScriptComponentVariable(true)]
        public Team Team = Team.Attacker;

        protected override void OnPreInit()
        {
            base.OnPreInit();
            _spawnerMissionHelper = new SpawnerEntityMissionHelper(this);
        }

        public override void AssignParameters(SpawnerEntityMissionHelper spawnerMissionHelper)
        {
            Falconet falconet = spawnerMissionHelper.SpawnedEntity.GetFirstScriptInFamilyDescending<Falconet>();
            falconet.SetSide(Team.Equals(Team.Attacker) ? BattleSideEnum.Attacker : BattleSideEnum.Defender);
            // falconet.SetForcedUse(true);
            // spawnerMissionHelper.SpawnedEntity.GetFirstScriptOfType<Ballista>().RemoveOnDeployTag = RemoveOnDeployTag;
            // spawnerMissionHelper.SpawnedEntity.GetFirstScriptOfType<Ballista>().HorizontalDirectionRestriction =
            //     DirectionRestrictionDegree * ((float)Math.PI / 180f);
        }
    }
}