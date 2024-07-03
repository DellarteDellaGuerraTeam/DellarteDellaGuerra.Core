namespace DellarteDellaGuerra.Domain.EquipmentPool
{
    /**
     * <summary>
     * This interface provides a way to define the mission equipment of a troop into multiple groups. 
     * </summary>
     */
    public interface IGetEquipmentPool
    {
        /**
         * <summary>
         *     Get the spawn equipment pool for a troop.
         * </summary>
         * <param name="troopId">The troop id</param>
         * <returns>The equipment pool for the troop</returns>
         */
        Model.EquipmentPool GetTroopEquipmentPool(string troopId);
    }
}
