using System;
using System.Reflection;
using DellarteDellaGuerra.Domain.EquipmentPool;
using DellarteDellaGuerra.SetSpawnEquipment.EquipmentPool.Mappers;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.SetSpawnEquipment.MissionLogic
{
    public class SpawnEquipmentInitialiser
    {
        private readonly IGetEquipmentPool _getEquipmentPool;
        private readonly EquipmentPoolMapper _equipmentPoolMapper;

        public SpawnEquipmentInitialiser(IGetEquipmentPool getEquipmentPool, EquipmentPoolMapper equipmentPoolMapper)
        {
            _getEquipmentPool = getEquipmentPool;
            _equipmentPoolMapper = equipmentPoolMapper;
        }

        public IMissionTroopSupplier DecorateWithTroopEquipmentSupplier(IMissionTroopSupplier supplier)
        {
            return new TroopEquipmentSupplier(supplier, _getEquipmentPool, _equipmentPoolMapper);
        }

        public void Initialise(Mission mission)
        {
            var spawnLogic = mission.GetMissionBehavior<IMissionAgentSpawnLogic>();
            if (spawnLogic is not null)
            {
                Process(spawnLogic);
                var missionLogicIndex =
                    mission.MissionLogics.FindIndex(missionLogic => missionLogic is IMissionAgentSpawnLogic);
                var missionBehaviourIndex =
                    mission.MissionBehaviors.FindIndex(missionBehaviour => missionBehaviour is IMissionAgentSpawnLogic);

                var newSpawnLogic = new CharacterSpawnCleanUpLogic();
                if (missionBehaviourIndex + 1 == mission.MissionBehaviors.Count)
                    mission.MissionBehaviors.Add(newSpawnLogic);
                else
                    mission.MissionBehaviors.Insert(missionBehaviourIndex + 1, newSpawnLogic);

                if (missionLogicIndex + 1 == mission.MissionLogics.Count)
                    mission.MissionLogics.Add(newSpawnLogic);
                else
                    mission.MissionLogics.Insert(missionLogicIndex + 1, newSpawnLogic);
            }
        }

        public void Process(IMissionAgentSpawnLogic agentSpawnLogic)
        {
            var fields = agentSpawnLogic.GetType()
                .GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

            foreach (var field in fields)

                if (field.FieldType.IsArray && field.FieldType.GetElementType().Name.Contains("MissionSide"))
                {
                    var missionSideArray = field.GetValue(agentSpawnLogic) as Array;
                    if (missionSideArray != null)
                        foreach (var missionSideInstance in missionSideArray)
                            if (missionSideInstance != null)
                                ProcessMissionSide(missionSideInstance);
                }
                else if (field.FieldType.Name.Contains("MissionSide"))
                {
                    var missionSideInstance = field.GetValue(agentSpawnLogic);
                    if (missionSideInstance != null) ProcessMissionSide(missionSideInstance);
                }
        }

        private void ProcessMissionSide(object missionSideInstance)
        {
            var fields = missionSideInstance.GetType()
                .GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

            foreach (var field in fields)
                if (typeof(IMissionTroopSupplier).IsAssignableFrom(field.FieldType))
                {
                    var troopSupplierInstance = field.GetValue(missionSideInstance) as IMissionTroopSupplier;
                    if (troopSupplierInstance != null &&
                        troopSupplierInstance.GetType() != typeof(TroopEquipmentSupplier))
                        field.SetValue(missionSideInstance, DecorateWithTroopEquipmentSupplier(troopSupplierInstance));
                }
        }
    }
}