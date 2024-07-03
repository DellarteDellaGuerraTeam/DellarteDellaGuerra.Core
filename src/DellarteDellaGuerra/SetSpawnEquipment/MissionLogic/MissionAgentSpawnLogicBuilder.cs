using System;
using DellarteDellaGuerra.Domain.EquipmentPool;
using DellarteDellaGuerra.SetSpawnEquipment.EquipmentPool.Mappers;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.TroopSuppliers;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.SetSpawnEquipment.MissionLogic
{
    /**
     * <summary>
     *     This class provides a way to build a MissionAgentSpawnLogic.
     * </summary>
     */
    public class MissionAgentSpawnLogicBuilder
    {
        private Func<MapEvent, BattleSideEnum, IMissionTroopSupplier> _decoratedTroopSupplier =
            GetDefaultMissionTroopSupplier;

        /**
         * <summary>
         *     This method creates a MissionAgentSpawnLogic instance with all the decorated behaviour.
         * </summary>
         * <returns>The MissionAgentSpawnLogic instance.</returns>
         * <remarks>
         *     The default behaviour is to use the native behaviour for the troop supplier.
         * </remarks>
         */
        public MissionAgentSpawnLogic Build()
        {
            // use MapEvent.PlayerMapEvent like the native behaviour
            var attacker = _decoratedTroopSupplier(MapEvent.PlayerMapEvent, BattleSideEnum.Attacker);
            var defender = _decoratedTroopSupplier(MapEvent.PlayerMapEvent, BattleSideEnum.Defender);
            return new CharacterSpawnCleanUpLogic(new[]
            {
                defender, attacker
                // use PartyBase.MainParty.Side like the native behaviour
            }, PartyBase.MainParty.Side, ResolveBattleSizeType());
        }

        /**
         * <summary>
         *     This method decoarates the MissionAgentSpawnLogic with the expanded equipment randomisation behaviour.
         * </summary>
         * <param name="getEquipmentPool">The troop equipment provider.</param>
         * <param name="equipmentPoolMapper">The equipment pool mapper.</param>
         * <returns>The MissionAgentSpawnLogicBuilder instance.</returns>
         */
        public MissionAgentSpawnLogicBuilder UseExpandedEquipmentRandomisation(IGetEquipmentPool getEquipmentPool,
            EquipmentPoolMapper equipmentPoolMapper)
        {
            _decoratedTroopSupplier = (mapEvent, battleSide) =>
                new TroopEquipmentSupplier(new PartyGroupTroopSupplier(mapEvent, battleSide),
                    getEquipmentPool, equipmentPoolMapper);
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
         *     This method resolves the battle size type of the mission.
         * </summary>
         * <returns>The battle size type of the mission.</returns>
         * <remarks>
         *     The implementation follows the native behaviour.
         * </remarks>
         */
        private Mission.BattleSizeType ResolveBattleSizeType()
        {
            if (Mission.Current.IsSiegeBattle) return Mission.BattleSizeType.Siege;
            if (Mission.Current.IsSallyOutBattle) return Mission.BattleSizeType.SallyOut;
            return Mission.BattleSizeType.Battle;
        }
    }
}