using DellarteDellaGuerra.Domain.Logging.Port;
using DellarteDellaGuerra.SpawnEquipmentPools.MissionBehaviours;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.SpawnEquipmentPools.MissionLogic
{
    /**
     * <summary>
     * This class provides a way to decorate a battle mission with new behaviour.
     * </summary>
     */
    public class DadgMission
    {
        private readonly ILogger _logger;

        public DadgMission(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<DadgMission>();
        }

        /**
         * <summary>
         * Decorates the mission with new behaviour.
         * </summary>
         * <remarks>
         * Currently, it expands equipment randomisation for each troop at spawn.
         * </remarks>
         */
        public void Decorate(Mission mission, ICampaignBehaviorManager campaignBehaviorManager)
        {
            if (mission.GetMissionBehavior<IMissionAgentSpawnLogic>() is null) return;

            var spawnLogic = GetSpawnLogic(campaignBehaviorManager);
            if (spawnLogic is not null)
            {
                OverrideMissionLogic<IMissionAgentSpawnLogic>(mission, spawnLogic);   
            }
        }

        private MissionAgentSpawnLogic? GetSpawnLogic(ICampaignBehaviorManager campaignBehaviorManager)
        {
            var troopEquipmentPoolsBehaviour = campaignBehaviorManager.GetBehavior<MissionTroopEquipmentCampaignBehaviour>();
            if (troopEquipmentPoolsBehaviour is null)
            {
                _logger.Error($"The CampaignBehaviorManager could not find {nameof(MissionTroopEquipmentCampaignBehaviour)}");
                return null;
            }
            return new MissionAgentSpawnLogicBuilder()
                .AddMissionTroopEquipmentProvider(troopEquipmentPoolsBehaviour)
                .Build();
        }

        private void OverrideMissionLogic<T>(Mission mission, TaleWorlds.MountAndBlade.MissionLogic newMissionLogic) where T : IMissionBehavior
        {
            int missionLogicIndex = mission.MissionLogics.FindIndex(missionLogic => missionLogic is T);
            int missionBehaviourIndex = mission.MissionBehaviors.FindIndex(missionBehaviour => missionBehaviour is T);

            if (missionLogicIndex < 0 && missionBehaviourIndex < 0)
            {
                _logger.Error($"Failed to override mission logic {typeof(T).Name} in mission scene {mission.SceneName}");
            }

            mission.RemoveMissionBehavior(mission.MissionBehaviors[missionBehaviourIndex]);
            mission.AddMissionBehavior(newMissionLogic);

            mission.MissionBehaviors.Remove(newMissionLogic);
            mission.MissionLogics.Remove(newMissionLogic);
            mission.MissionBehaviors.Insert(missionBehaviourIndex, newMissionLogic);
            mission.MissionLogics.Insert(missionLogicIndex, newMissionLogic);
        }
    }
}

