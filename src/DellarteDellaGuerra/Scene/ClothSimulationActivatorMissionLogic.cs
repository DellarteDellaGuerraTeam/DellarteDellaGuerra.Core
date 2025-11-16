using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Scene
{
    /// <summary>
    /// Needed to prevent the disabling of cloth simulation when heraldry texture is applied 
    /// to bd_banner_b tagged entities
    /// Unsure if this is still required for v1.3
    /// </summary>
    public class ClothSimulationActivatorMissionLogic : MissionLogic
    {
        public override void AfterStart()
        {
            base.AfterStart();

            Mission.Current.Scene.SetClothSimulationState(true);
        }
    }
}