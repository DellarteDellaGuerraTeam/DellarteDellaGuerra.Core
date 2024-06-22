using System;
using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.Equipment.Get;
using DellarteDellaGuerra.Equipment.Get.Mappers;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Equipment.Get.MissionLogic
{
    public class TroopEquipmentSpawnLogic : TaleWorlds.MountAndBlade.MissionLogic
    {
        private readonly IGetTroopEquipment _getTroopEquipment;
        private readonly EquipmentPoolMapper _equipmentPoolMapper;
        private readonly Random _random = new();

        public TroopEquipmentSpawnLogic(IGetTroopEquipment getTroopEquipment, EquipmentPoolMapper equipmentPoolMapper)
        {
            _getTroopEquipment = getTroopEquipment;
            _equipmentPoolMapper = equipmentPoolMapper;
        }

        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            base.OnAgentBuild(agent, banner);
            if (agent?.IsMount ?? true) return;

            var equipmentPool = GetRandomEquipmentPool(agent.Origin);
            var equipment = GetRandomEquipmentLoadout(equipmentPool);
            agent.ClearEquipment();
            agent.UpdateSpawnEquipmentAndRefreshVisuals(equipment);
        }

        private TaleWorlds.Core.Equipment GetRandomEquipmentLoadout(MBEquipmentRoster equipmentPool)
        {
            equipmentPool.AllEquipments.Shuffle();
            return equipmentPool.AllEquipments[_random.Next(0, equipmentPool.AllEquipments.Count)];
        }

        private MBEquipmentRoster GetRandomEquipmentPool(IAgentOriginBase agent)
        {
            // Creates a list of equipment pools where each pool is present as many times as the number of equipments it contains 
            IList<MBEquipmentRoster> equipmentWeightedPools =
                _equipmentPoolMapper.MapEquipmentPool(_getTroopEquipment
                        .GetEquipmentPools(agent.Troop.StringId), agent.Troop.StringId)
                    .SelectMany(pool => pool.AllEquipments, (pool, equipment) => (pool, equipment))
                    .Select(e => e.pool)
                    .ToList();

            // If no equipment pools are defined for the troop, then the native roster should be used
            if (equipmentWeightedPools.IsEmpty())
                return new MBEquipmentRoster();

            equipmentWeightedPools.Shuffle();
            var randomIndex = _random.Next(0, equipmentWeightedPools.Count);
            return equipmentWeightedPools[randomIndex];
        }
    }
}