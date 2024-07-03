using System.Collections.Generic;

namespace DellarteDellaGuerra.Domain.EquipmentPool.Port
{
    public interface ITroopCivilianEquipmentProvider
    {
        IList<Model.EquipmentPool> GetCivilianTroopEquipmentPools(string troopId);
    }
}