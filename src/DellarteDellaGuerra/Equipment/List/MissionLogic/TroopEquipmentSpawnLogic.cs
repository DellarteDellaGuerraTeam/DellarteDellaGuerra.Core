using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace DellarteDellaGuerra.Equipment.List.MissionLogic
{
    public class TroopEquipmentSpawnLogic : TaleWorlds.MountAndBlade.MissionLogic
    {
        private readonly IMissionTroopEquipmentProvider _troopBattleEquipmentProvider;
        private readonly Random _random = new();

        public TroopEquipmentSpawnLogic(IMissionTroopEquipmentProvider troopBattleBattleEquipmentProvider)
        {
            _troopBattleEquipmentProvider = troopBattleBattleEquipmentProvider;
        }

        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            base.OnAgentBuild(agent, banner);
            if (agent?.IsMount ?? true) return;

            var equipmentPool = GetRandomEquipmentPool(agent.Origin);
            var equipment = GetRandomEquipmentLoadout(equipmentPool);
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
                _troopBattleEquipmentProvider
                    .GetTroopEquipmentPools(agent.Troop)
                    .SelectMany(pool => pool.AllEquipments, (pool, equipment) => (pool, equipment))
                    .Where(e => !e.equipment.IsCivilian)
                    .Select(e => e.pool)
                    .ToList();

            // If no equipment pools are defined for the troop, then the native roster should be used
            if (equipmentWeightedPools.IsEmpty())
                return MBObjectManager.Instance.GetObject<MBEquipmentRoster>(agent.Troop.StringId);

            equipmentWeightedPools.Shuffle();
            var randomIndex = _random.Next(0, equipmentWeightedPools.Count);
            return equipmentWeightedPools[randomIndex];
        }
    }
}