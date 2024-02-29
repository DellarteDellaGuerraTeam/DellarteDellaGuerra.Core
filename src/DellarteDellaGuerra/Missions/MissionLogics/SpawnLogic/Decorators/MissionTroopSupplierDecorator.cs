using System.Collections.Generic;
using TaleWorlds.Core;

namespace DellarteDellaGuerra.Missions.MissionLogics.SpawnLogic.Decorators
{
    /**
     * <summary>
     * This IMissionTroopSupplier decorator provides a way to create new ways to supply troops for a mission.
     * </summary>
     */
    public class MissionTroopSupplierDecorator : IMissionTroopSupplier
    {
        private readonly IMissionTroopSupplier _missionTroopSupplier;

        protected MissionTroopSupplierDecorator(IMissionTroopSupplier missionTroopSupplier)
        {
            _missionTroopSupplier = missionTroopSupplier;
        }

        public virtual IEnumerable<IAgentOriginBase> SupplyTroops(int numberToAllocate) => _missionTroopSupplier.SupplyTroops(numberToAllocate);

        public virtual IEnumerable<IAgentOriginBase> GetAllTroops() => _missionTroopSupplier.GetAllTroops();

        public virtual BasicCharacterObject GetGeneralCharacter() => _missionTroopSupplier.GetGeneralCharacter();

        public virtual int GetNumberOfPlayerControllableTroops() => _missionTroopSupplier.GetNumberOfPlayerControllableTroops();

        public virtual int NumRemovedTroops => _missionTroopSupplier.NumRemovedTroops;

        public virtual int NumTroopsNotSupplied => _missionTroopSupplier.NumTroopsNotSupplied;

        public virtual bool AnyTroopRemainsToBeSupplied => _missionTroopSupplier.AnyTroopRemainsToBeSupplied;
    }
}
