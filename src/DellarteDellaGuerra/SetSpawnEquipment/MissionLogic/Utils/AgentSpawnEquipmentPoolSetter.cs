using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.EquipmentPool;
using DellarteDellaGuerra.SetSpawnEquipment.EquipmentPool.Mappers;
using DellarteDellaGuerra.SetSpawnEquipment.Ports;
using Force.DeepCloner;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace DellarteDellaGuerra.SetSpawnEquipment.MissionLogic.Utils
{
    public class AgentSpawnEquipmentPoolSetter
    {
        private readonly Dictionary<int, IAgentOriginBase> _agents = new();
        private readonly IGetEquipmentPool _getEquipmentPool;
        private readonly EquipmentPoolMapper _equipmentPoolMapper;
        private readonly IMissionEventCallbackRegister _missionEventCallbackRegister;
        private readonly MBObjectManager _mbObjectManager;

        public AgentSpawnEquipmentPoolSetter(
            IGetEquipmentPool getEquipmentPool, EquipmentPoolMapper equipmentPoolMapper,
            IMissionEventCallbackRegister missionEventCallbackRegister,
            MBObjectManager mbObjectManager)
        {
            _getEquipmentPool = getEquipmentPool;
            _equipmentPoolMapper = equipmentPoolMapper;
            _missionEventCallbackRegister = missionEventCallbackRegister;
            _mbObjectManager = mbObjectManager;
        }

        public IAgentOriginBase ResolveEquipment(IAgentOriginBase agent)
        {
            if (!_agents.ContainsKey(agent.UniqueSeed))
                _agents[agent.UniqueSeed] = OverrideAgentEquipment(agent, GetRandomEquipmentPool(agent));

            return _agents[agent.UniqueSeed];
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
            var equipment = agent.Troop.Equipment.Clone();
            var originalEquipment = agent.Troop.Equipment;
            _missionEventCallbackRegister.RegisterMissionEndedCallback(() =>
            {
                var a = agent.Troop;
                originalEquipment.FillFrom(equipment);
            });
            // _missionEventCallbackRegister.RegisterOnAgentBuildCallback(agent.UniqueSeed,
            //     (_, _) => originalEquipment.FillFrom(equipment));

            // TODO: fix bug when AllEquipment is empty
            agent.Troop.Equipment.FillFrom(equipmentPool.AllEquipments.Count > 0
                ? equipmentPool.AllEquipments[0]
                : new Equipment());
            return agent;
        }

        /**
         * In Native, the troops with the same id use the same character object reference.
         * So we create a new character object for each agent so that they may have different equipment pools.
         * We make sure to reset the agent's character with the global character object when the agent has been built.
         */
        private IAgentOriginBase OverrideTroopEquipment(IAgentOriginBase agent, MBEquipmentRoster equipmentPool)
        {
            // the character object is shared among all agents
            // so we do not want to modify the original character state
            var character = agent.Troop.DeepClone();

            // set all equipment from pool to character by reference
            var equipments = character.AllEquipments;
            equipments.Clear();
            equipmentPool.AllEquipments.ForEach(equipments.Add);

            // reset the global character object to the original character object after the agent was built
            // _missionEventCallbackRegister.RegisterOnAgentBuildCallback((builtAgent, _) =>
            // {
            //     SetGlobalTroopCharacter(builtAgent);
            // });
            _missionEventCallbackRegister.RegisterOnAgentBuildCallback(agent.UniqueSeed,
                (builtAgent, _) => { SetGlobalTroopCharacter(builtAgent); });


            // override the agent origin with the new character
            return new AgentOriginTroopOverrider(agent)
            {
                Troop = character
            };
        }

        private void SetGlobalTroopCharacter(Agent agent)
        {
            if (agent?.Character?.StringId is null) return;

            var globalCharacterObject =
                _mbObjectManager.GetObject<BasicCharacterObject>(agent.Character.StringId);
            if (globalCharacterObject is null || agent.Character == globalCharacterObject) return;

            agent.Character = globalCharacterObject;

            if (agent.Origin is null) return;

            agent.Origin = new AgentOriginTroopOverrider(agent.Origin)
            {
                Troop = globalCharacterObject
            };
        }

        private MBEquipmentRoster GetRandomEquipmentPool(IAgentOriginBase agent)
        {
            var equipmentPool = _getEquipmentPool.GetTroopEquipmentPool(agent.Troop.StringId);
            return _equipmentPoolMapper.MapEquipmentPool(equipmentPool, agent.Troop.StringId);
        }
    }
}