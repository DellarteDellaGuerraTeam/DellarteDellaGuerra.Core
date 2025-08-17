using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Objects.Siege;

namespace DellarteDellaGuerra.Cannon
{
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

        protected override void OnPreInit()
        {
            base.OnPreInit();
            _spawnerMissionHelper = new SpawnerEntityMissionHelper(this);
            _spawnerMissionHelperFire = new SpawnerEntityMissionHelper(this, true);
        }

        public override void AssignParameters(SpawnerEntityMissionHelper _spawnerMissionHelper)
        {
            // _spawnerMissionHelper.SpawnedEntity.GetFirstScriptOfType<Falconet>().AddOnDeployTag = AddOnDeployTag;
            // _spawnerMissionHelper.SpawnedEntity.GetFirstScriptOfType<Ballista>().RemoveOnDeployTag = RemoveOnDeployTag;
            // _spawnerMissionHelper.SpawnedEntity.GetFirstScriptOfType<Ballista>().HorizontalDirectionRestriction =
            //     DirectionRestrictionDegree * ((float)Math.PI / 180f);
        }
    }
}