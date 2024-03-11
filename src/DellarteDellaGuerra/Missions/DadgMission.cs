using DellarteDellaGuerra.Missions.MissionLogics.SpawnLogic;
using DellarteDellaGuerra.DadgCampaign.Behaviours;
using DellarteDellaGuerra.Logging;
using NLog;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Missions
{
    /**
     * <summary>
     * This class provides a way to decorate a battle mission with new behaviour.
     * </summary>
     */
    public class DadgMission
    {
        private static readonly Logger Logger = LoggerFactory.GetLogger<DadgMission>();

        private DadgMission() { }

        /**
         * <summary>
         * Decorates the mission with new behaviour.
         * </summary>
         * <remarks>
         * Currently, it expands equipment randomisation for each troop at spawn.
         * </remarks>
         */
        public static void Decorate(Mission mission, ICampaignBehaviorManager campaignBehaviorManager)
        {
            if (mission.GetMissionBehavior<IMissionAgentSpawnLogic>() is null) return;

            var spawnLogic = GetSpawnLogic(campaignBehaviorManager);
            if (spawnLogic is not null)
            {
                OverrideMissionLogic<IMissionAgentSpawnLogic>(mission, spawnLogic);   
            }
        }

        private static MissionAgentSpawnLogic? GetSpawnLogic(ICampaignBehaviorManager campaignBehaviorManager)
        {
            var troopEquipmentPoolsBehaviour = campaignBehaviorManager.GetBehavior<MissionTroopEquipmentCampaignBehaviour>();
            if (troopEquipmentPoolsBehaviour is null)
            {
                Logger.Error("The CampaignBehaviorManager could not find {0}", nameof(MissionTroopEquipmentCampaignBehaviour));
                return null;
            }
            return new MissionAgentSpawnLogicBuilder()
                .AddMissionTroopEquipmentProvider(troopEquipmentPoolsBehaviour)
                .Build();
        }

        private static void OverrideMissionLogic<T>(Mission mission, MissionLogic newMissionLogic) where T : IMissionBehavior
        {
            int missionLogicIndex = mission.MissionLogics.FindIndex(missionLogic => missionLogic is T);
            int missionBehaviourIndex = mission.MissionBehaviors.FindIndex(missionBehaviour => missionBehaviour is T);

            if (missionLogicIndex < 0 && missionBehaviourIndex < 0)
            {
                Logger.Error($"Failed to override mission logic {typeof(T).Name} in mission scene {mission.SceneName}");
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

