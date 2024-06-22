using DellarteDellaGuerra.Domain.Common.Logging.Port;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Infrastructure.Missions
{
    /**
     * <summary>
     *     This class provides a way to decorate a battle mission with new behaviour.
     * </summary>
     */
    public class MissionLogicOverrider
    {
        private readonly ILogger _logger;

        public MissionLogicOverrider(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<MissionLogicOverrider>();
        }

        /**
         * <summary>
         *     Decorates the mission with new behaviour.
         * </summary>
         * <remarks>
         *     Currently, it expands equipment randomisation for each troop at spawn.
         * </remarks>
         */
        // public void Decorate(Mission mission, IGetTroopEquipment getTroopEquipment,
        //     EquipmentPoolMapper equipmentPoolMapper)
        // {
        //     if (mission.GetMissionBehavior<IMissionAgentSpawnLogic>() is null) return;
        //
        //     var spawnLogic = GetSpawnLogic(getTroopEquipment, equipmentPoolMapper);
        //     OverrideMissionLogic<IMissionAgentSpawnLogic>(mission, spawnLogic);
        // }
        public void OverrideMissionLogic<T>(Mission mission, MissionLogic newMissionLogic)
            where T : IMissionBehavior
        {
            var missionLogicIndex = mission.MissionLogics.FindIndex(missionLogic => missionLogic is T);
            var missionBehaviourIndex = mission.MissionBehaviors.FindIndex(missionBehaviour => missionBehaviour is T);

            if (missionLogicIndex < 0 && missionBehaviourIndex < 0)
                _logger.Error(
                    $"Failed to override mission logic {typeof(T).Name} in mission scene {mission.SceneName}");

            mission.RemoveMissionBehavior(mission.MissionBehaviors[missionBehaviourIndex]);
            mission.AddMissionBehavior(newMissionLogic);

            mission.MissionBehaviors.Remove(newMissionLogic);
            mission.MissionLogics.Remove(newMissionLogic);
            mission.MissionBehaviors.Insert(missionBehaviourIndex, newMissionLogic);
            mission.MissionLogics.Insert(missionLogicIndex, newMissionLogic);
        }
    }
}