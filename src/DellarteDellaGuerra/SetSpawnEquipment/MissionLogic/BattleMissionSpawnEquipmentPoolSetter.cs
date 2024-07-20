using System;
using System.Linq;
using System.Reflection;
using DellarteDellaGuerra.SetSpawnEquipment.MissionLogic.Utils;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.SetSpawnEquipment.MissionLogic
{
    public class BattleMissionSpawnEquipmentPoolSetter : TaleWorlds.MountAndBlade.MissionLogic
    {
        private readonly AgentSpawnEquipmentPoolSetter _agentSpawnEquipmentPoolSetter;

        public BattleMissionSpawnEquipmentPoolSetter(AgentSpawnEquipmentPoolSetter agentSpawnEquipmentPoolSetter)
        {
            _agentSpawnEquipmentPoolSetter = agentSpawnEquipmentPoolSetter;
        }

        public override void OnBehaviorInitialize()
        {
            var spawnLogic = Mission.GetMissionBehavior<IMissionAgentSpawnLogic>();
            if (spawnLogic is null)
                return;
            ProcessSpawnLogic(spawnLogic);
        }

        private IMissionTroopSupplier DecorateTroopSupplier(IMissionTroopSupplier supplier)
        {
            return new TroopEquipmentSupplier(supplier, _agentSpawnEquipmentPoolSetter);
        }

        private void ProcessSpawnLogic(IMissionAgentSpawnLogic agentSpawnLogic)
        {
            agentSpawnLogic.GetType()
                .GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
                .ToList()
                .ForEach(field =>
                {
                    if (field.FieldType.IsArray &&
                        (field.FieldType.GetElementType()?.Name.Contains("MissionSide") ?? false))
                    {
                        (field.GetValue(agentSpawnLogic) as Array)?.OfType<object>().ToList()
                            .ForEach(ProcessMissionSide);
                    }
                    else if (field.FieldType.Name.Contains("MissionSide"))
                    {
                        var missionSideInstance = field.GetValue(agentSpawnLogic);
                        if (missionSideInstance != null) ProcessMissionSide(missionSideInstance);
                    }
                });
        }

        private void ProcessMissionSide(object missionSideInstance)
        {
            missionSideInstance.GetType()
                .GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
                .Where(field => typeof(IMissionTroopSupplier).IsAssignableFrom(field.FieldType) &&
                                field.GetValue(missionSideInstance) is not TroopEquipmentSupplier)
                .ToList()
                .ForEach(field => field.SetValue(missionSideInstance,
                    DecorateTroopSupplier((IMissionTroopSupplier)field.GetValue(missionSideInstance))));
        }
    }
}