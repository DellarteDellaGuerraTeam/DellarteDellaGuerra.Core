using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.EquipmentPool;
using DellarteDellaGuerra.SetSpawnEquipment.EquipmentPool.Mappers;
using DellarteDellaGuerra.SetSpawnEquipment.MissionLogic.Utils;
using Force.DeepCloner;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace DellarteDellaGuerra.SetSpawnEquipment.MissionLogic
{
    /**
     * <summary>
     *     This MissionTroopSupplier sets the domain-provided equipment pool for each troop and lords.
     * </summary>
     */
    public class TroopEquipmentSupplier : IMissionTroopSupplier
    {
        private readonly Dictionary<int, MBEquipmentRoster> _agentEquipmentPool = new();
        private readonly IGetEquipmentPool _getEquipmentPool;
        private readonly EquipmentPoolMapper _equipmentPoolMapper;
        private readonly IMissionTroopSupplier _missionTroopSupplier;

        public TroopEquipmentSupplier(
            IMissionTroopSupplier missionTroopSupplier,
            IGetEquipmentPool getEquipmentPool, EquipmentPoolMapper equipmentPoolMapper)
        {
            _missionTroopSupplier = missionTroopSupplier;
            _getEquipmentPool = getEquipmentPool;
            _equipmentPoolMapper = equipmentPoolMapper;
        }

        private IAgentOriginBase ResolveEquipment(IAgentOriginBase agent)
        {
            if (!_agentEquipmentPool.ContainsKey(agent.UniqueSeed))
                _agentEquipmentPool[agent.UniqueSeed] = GetRandomEquipmentPool(agent);

            return OverrideAgentEquipment(agent, _agentEquipmentPool[agent.UniqueSeed]);
        }

        private IAgentOriginBase OverrideAgentEquipment(IAgentOriginBase agent, MBEquipmentRoster equipmentPool)
        {
            if (Clan.PlayerClan.Heroes.Any(hero => agent.Troop.StringId == hero.StringId))
                return agent;

            return agent.Troop.IsHero
                ? OverrideLordEquipment(agent, equipmentPool)
                : OverrideTroopEquipment(agent, equipmentPool);
        }

        private IAgentOriginBase OverrideLordEquipment(IAgentOriginBase agent, MBEquipmentRoster equipmentPool)
        {
            agent.Troop.Equipment.FillFrom(equipmentPool.AllEquipments.Count > 0
                ? equipmentPool.AllEquipments[0]
                : new Equipment());
            return agent;
        }

        private IAgentOriginBase OverrideTroopEquipment(IAgentOriginBase agent, MBEquipmentRoster equipmentPool)
        {
            // the character object is shared among all agents
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
            var equipmentPool = _getEquipmentPool.GetTroopEquipmentPool(agent.Troop.StringId);
            return _equipmentPoolMapper.MapEquipmentPool(equipmentPool, agent.Troop.StringId);
        }

        #region IMissionTroopSupplier

        public IEnumerable<IAgentOriginBase> SupplyTroops(int numberToAllocate)
        {
            return _missionTroopSupplier.SupplyTroops(numberToAllocate).Select(ResolveEquipment);
        }

        public IEnumerable<IAgentOriginBase> GetAllTroops()
        {
            return _missionTroopSupplier.GetAllTroops().Select(ResolveEquipment);
        }

        public BasicCharacterObject GetGeneralCharacter()
        {
            return _missionTroopSupplier.GetGeneralCharacter();
        }

        public int GetNumberOfPlayerControllableTroops()
        {
            return _missionTroopSupplier.GetNumberOfPlayerControllableTroops();
        }

        public int NumRemovedTroops => _missionTroopSupplier.NumRemovedTroops;

        public int NumTroopsNotSupplied => _missionTroopSupplier.NumTroopsNotSupplied;

        public bool AnyTroopRemainsToBeSupplied => _missionTroopSupplier.AnyTroopRemainsToBeSupplied;

        #endregion
    }
}