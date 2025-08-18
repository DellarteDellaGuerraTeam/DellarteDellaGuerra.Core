using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Objects.Siege;

namespace DellarteDellaGuerra.Cannon
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

            var a = falconet.Team;

            falconet.SetSide(Team.Equals(Team.Attacker) ? BattleSideEnum.Attacker : BattleSideEnum.Defender);
            falconet.SetForcedUse(true);
            // spawnerMissionHelper.SpawnedEntity.GetFirstScriptOfType<Ballista>().RemoveOnDeployTag = RemoveOnDeployTag;
            // spawnerMissionHelper.SpawnedEntity.GetFirstScriptOfType<Ballista>().HorizontalDirectionRestriction =
            //     DirectionRestrictionDegree * ((float)Math.PI / 180f);
        }

        private TaleWorlds.MountAndBlade.Team? GetTeam()
        {
            if (Mission.Current?.Teams?.Attacker is null || Mission.Current?.Teams?.Defender is null) return null;

            return Team.Equals(Team.Attacker) ? Mission.Current.Teams.Attacker : Mission.Current.Teams.Defender;
        }
    }
}