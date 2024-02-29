using System;
using DellarteDellaGuerra.Missions.MissionLogics.SpawnLogic.Decorators;
using DellarteDellaGuerra.Missions.MissionLogics.SpawnLogic.Decorators.Support;
using DellarteDellaGuerra.DadgCampaign.Behaviours;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.TroopSuppliers;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace DellarteDellaGuerra.Missions.MissionLogics.SpawnLogic
{
    /**
     * <summary>
     * This class provides a way to build a MissionAgentSpawnLogic.
     * </summary>
     */
    public class MissionAgentSpawnLogicBuilder
    {
        private Func<MapEvent, BattleSideEnum, IMissionTroopSupplier> _decoratedTroopSupplier = GetDefaultMissionTroopSupplier;

        /**
         * <summary>
         * This method creates a MissionAgentSpawnLogic instance with all the decorated behaviour.
         * </summary>
         * <returns>The MissionAgentSpawnLogic instance.</returns>
         * <remarks>
         * The default behaviour is to use the native behaviour for the troop supplier.
         * </remarks>
         */
        public MissionAgentSpawnLogic Build()
        {
            // use MapEvent.PlayerMapEvent like the native behaviour
            var attacker = _decoratedTroopSupplier(MapEvent.PlayerMapEvent, BattleSideEnum.Attacker);
            var defender = _decoratedTroopSupplier(MapEvent.PlayerMapEvent, BattleSideEnum.Defender);
            return new CharacterSpawnCleanUpLogic(new []
            {
                defender, attacker
                // use PartyBase.MainParty.Side like the native behaviour
            }, PartyBase.MainParty.Side, ResolveBattleSizeType());
        }

        /**
         * <summary>
         * This method decoarates the MissionAgentSpawnLogic with the expanded equipment randomisation behaviour.
         * </summary>
         * <param name="troopEquipmentPoolsBehaviour">The troop equipment pools behaviour.</param>
         * <returns>The MissionAgentSpawnLogicBuilder instance.</returns>
         */
        public MissionAgentSpawnLogicBuilder UseExpandedEquipmentRandomisation(TroopEquipmentPoolsCampaignBehaviour troopEquipmentPoolsBehaviour)
        {
            var decoratedMissionTroopSupplier = _decoratedTroopSupplier;
            _decoratedTroopSupplier = (mapEvent, battleSide) => 
                new TroopEquipmentRandomisationDecorator(decoratedMissionTroopSupplier(mapEvent, battleSide), troopEquipmentPoolsBehaviour);
            return this;
        }

        /**
         * <returns>The default IMissionTroopSupplier instance.</returns>
         */
        private static IMissionTroopSupplier GetDefaultMissionTroopSupplier(MapEvent mapEvent, BattleSideEnum side)
        {
            return new PartyGroupTroopSupplier(mapEvent, side);
        }

        /**
         * <summary>
         * This method resolves the battle size type of the mission.
         * </summary>
         * <returns>The battle size type of the mission.</returns>
         * <remarks>
         * The implementation follows the native behaviour.
         * </remarks>
         */
        private Mission.BattleSizeType ResolveBattleSizeType()
        {
            if (Mission.Current.IsSiegeBattle)
            {
                return Mission.BattleSizeType.Siege;
            }
            if (Mission.Current.IsSallyOutBattle)
            {
                return Mission.BattleSizeType.SallyOut;
            }
            return Mission.BattleSizeType.Battle;
        }

        private sealed class CharacterSpawnCleanUpLogic : MissionAgentSpawnLogic
        {
            public CharacterSpawnCleanUpLogic(
                IMissionTroopSupplier[] missionTroopSuppliers,
                BattleSideEnum playerSide,
                Mission.BattleSizeType battleSizeType) : base(missionTroopSuppliers, playerSide, battleSizeType)
            {
            }

            /**
             * <summary>
             * This method is called when an agent is built.
             * <br/>
             * Natively, the troops with the same id reference the same character object. 
             * So here, we make sure to override the agent's character with the global character object if needed.
             * </summary>
             * <param name="agent">The agent to build.</param>
             * <param name="banner">The banner of the agent.</param>
             * <remarks>
             * This method is called when an agent is built and should not be called else where.
             * </remarks>
             */
            public override void OnAgentBuild(Agent agent, Banner banner)
            {
                base.OnAgentBuild(agent, banner);

                if (agent?.Character?.StringId is null) return;

                var globalCharacterObject = MBObjectManager.Instance.GetObject<BasicCharacterObject>(agent.Character.StringId);
                if (globalCharacterObject is null || agent.Character == globalCharacterObject) return;
                
                agent.Character = globalCharacterObject;

                if (agent.Origin is null) return;

                agent.Origin = new AgentOriginTroopOverrider(agent.Origin)
                {
                    Troop = globalCharacterObject
                };
            }
        }
    }
}

