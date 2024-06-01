using System.Collections.Generic;
using TaleWorlds.Core;

namespace DellarteDellaGuerra.SpawnEquipmentPools.MissionLogic
{
    /**
     * <summary>
     * This interface provides a way to define the mission equipment of a troop into multiple groups. 
     * </summary>
     */
    public interface IMissionTroopEquipmentProvider
    {
        /**
         * <summary>
         * Get the equipment pools for a troop.
         * </summary>
         * <param name="character">The troop character</param>
         * <returns>The equipment pools for the troop</returns>
         */
        IEnumerable<MBEquipmentRoster> GetTroopEquipmentPools(BasicCharacterObject character);
    }
}
