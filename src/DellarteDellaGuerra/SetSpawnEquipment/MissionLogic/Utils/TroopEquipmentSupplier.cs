using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;

namespace DellarteDellaGuerra.SetSpawnEquipment.MissionLogic.Utils
{
    /**
     * <summary>
     *     This MissionTroopSupplier sets the domain-provided equipment pool for each troop and lords.
     * </summary>
     */
    public class TroopEquipmentSupplier : IMissionTroopSupplier
    {
        private readonly AgentSpawnEquipmentPoolSetter _agentSpawnEquipmentPoolSetter;
        private readonly IMissionTroopSupplier _missionTroopSupplier;

        public TroopEquipmentSupplier(
            IMissionTroopSupplier missionTroopSupplier,
            AgentSpawnEquipmentPoolSetter agentSpawnEquipmentPoolSetter)
        {
            _missionTroopSupplier = missionTroopSupplier;
            _agentSpawnEquipmentPoolSetter = agentSpawnEquipmentPoolSetter;
        }

        public IEnumerable<IAgentOriginBase> SupplyTroops(int numberToAllocate)
        {
            return _missionTroopSupplier.SupplyTroops(numberToAllocate)
                .Select(_agentSpawnEquipmentPoolSetter.ResolveEquipment);
        }

        public IEnumerable<IAgentOriginBase> GetAllTroops()
        {
            return _missionTroopSupplier.GetAllTroops().Select(_agentSpawnEquipmentPoolSetter.ResolveEquipment);
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
    }
}