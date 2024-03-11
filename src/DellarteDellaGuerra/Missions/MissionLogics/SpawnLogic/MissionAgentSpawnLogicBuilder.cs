using DellarteDellaGuerra.Missions.MissionLogics.SpawnLogic.Adapters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.TroopSuppliers;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Missions.MissionLogics.SpawnLogic
{
    /**
     * <summary>
     * This class provides a way to build a MissionAgentSpawnLogic.
     * </summary>
     */
    public class MissionAgentSpawnLogicBuilder
    {
        private IMissionTroopEquipmentProvider? _equipmentPoolProvider;

        /**
         * <summary>
         * This method creates a MissionAgentSpawnLogic instance with the additional behaviours provided by the builder.
         * </summary>
         * <returns>The MissionAgentSpawnLogic instance.</returns>
         * <remarks>
         * The default behaviour is to use the native behaviour for the troop supplier.
         * </remarks>
         */
        public MissionAgentSpawnLogic Build()
        {
            return new CharacterSpawnCleanUpLogic(new []
            {
                ResolveDecoratedMissionTroopSupplier(BattleSideEnum.Defender),
                ResolveDecoratedMissionTroopSupplier(BattleSideEnum.Attacker),
                // use PartyBase.MainParty.Side like the native behaviour
            }, PartyBase.MainParty.Side, ResolveBattleSizeType());
        }

        /**
         * <summary>
         * Add a provider enabling more control over the equipment pool of the troops.
         * </summary>
         * <param name="missionTroopEquipmentProvider">The troop equipment pool provider.</param>
         * <returns>The MissionAgentSpawnLogicBuilder instance.</returns>
         */
        public MissionAgentSpawnLogicBuilder AddMissionTroopEquipmentProvider(IMissionTroopEquipmentProvider missionTroopEquipmentProvider)
        {
            _equipmentPoolProvider = missionTroopEquipmentProvider;
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

        /**
         * <summary>
         * This method adapts the given IMissionEquipmentProvider to the IMissionTroopSupplier interface.
         * It returns the defaults IMissionTroopSupplier if the IMissionEquipmentProvider was not given.
         * </summary>
         */
        private IMissionTroopSupplier ResolveDecoratedMissionTroopSupplier(BattleSideEnum battleSide)
        {
            var missionTroopSupplier = GetDefaultMissionTroopSupplier(MapEvent.PlayerMapEvent, battleSide);
            return _equipmentPoolProvider is null ? 
                missionTroopSupplier :
                new MissionEquipmentTroopProviderAdapter(_equipmentPoolProvider, missionTroopSupplier);
        }
    }
}
