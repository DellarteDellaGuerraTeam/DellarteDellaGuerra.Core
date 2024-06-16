using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Equipment.Get.Model;

namespace DellarteDellaGuerra.Domain.Equipment.Get
{
    /**
     * <summary>
     * This interface provides a way to define the mission equipment of a troop into multiple groups. 
     * </summary>
     */
    public interface IGetTroopEquipment
    {
        /**
         * <summary>
         * Get the equipment pools for a troop.
         * </summary>
         * <param name="troopId">The troop id</param>
         * <returns>The equipment pools for the troop</returns>
         */
        IList<EquipmentPool> GetEquipmentPools(string troopId);
    }
}
