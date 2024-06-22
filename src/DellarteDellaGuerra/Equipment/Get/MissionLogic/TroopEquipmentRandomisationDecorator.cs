using System;
using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.Equipment.Get;
using DellarteDellaGuerra.Equipment.Get.Mappers;
using DellarteDellaGuerra.Equipment.Get.MissionLogic.Utils;
using Force.DeepCloner;
using TaleWorlds.Core;

namespace DellarteDellaGuerra.Equipment.Get.MissionLogic
{
    /**
     * <summary>
     *     The native behaviour basically mixes all equipment sets together and selects randomly, for each item category,
     *     one item among all equipment sets.
     *     <br />
     *     This MissionTroopSupplier decorator provides a way to create new ways to randomise equipment for each troop.
     * </summary>
     * <remarks>
     *     Each troop will randomly select an equipment pool to use in the battle.
     *     <br />
     *     <br />
     *     If no groups are defined for a troop, then the native behaviour is used.
     *     <br />
     *     <br />
     *     On the other hand, if each group has one and only one equipment, then the troop will randomly select an equipment
     *     set.
     * </remarks>
     */
    public class TroopEquipmentRandomisationDecorator : MissionTroopSupplierDecorator
    {
        private readonly Random _random = new();
        private readonly IGetTroopEquipment _getTroopEquipment;
        private readonly EquipmentPoolMapper _equipmentPoolMapper;

        public TroopEquipmentRandomisationDecorator(
            IMissionTroopSupplier missionTroopSupplier,
            IGetTroopEquipment getTroopEquipment, EquipmentPoolMapper equipmentPoolMapper) : base(missionTroopSupplier)
        {
            _getTroopEquipment = getTroopEquipment;
            _equipmentPoolMapper = equipmentPoolMapper;
        }

        public override IEnumerable<IAgentOriginBase> SupplyTroops(int numberToAllocate)
        {
            return base.SupplyTroops(numberToAllocate).Select(ResolveEquipment);
        }

        public override IEnumerable<IAgentOriginBase> GetAllTroops()
        {
            return base.GetAllTroops().Select(ResolveEquipment);
        }

        private IAgentOriginBase ResolveEquipment(IAgentOriginBase agent)
        {
            return OverrideAgentEquipment(agent, GetRandomEquipmentPool(agent));
        }

        private IAgentOriginBase OverrideAgentEquipment(IAgentOriginBase agent, MBEquipmentRoster equipmentPool)
        {
            // the character state is shared among all agents
            // so we do not want to modify the original character state
            var character = agent.Troop.DeepClone();

            // set all equipment from pool to character by reference
            var equipments = character.AllEquipments;
            equipments.Clear();
            equipmentPool.AllEquipments.ForEach(equipments.Add);

            // override the agent origin with the new character
            return new AgentOriginTroopOverrider(agent)
            {
                Troop = character
            };
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