using System.Collections.Generic;

namespace DellarteDellaGuerra.Domain.EquipmentPool.Port
{
    public interface ITroopSiegeEquipmentProvider
    {
        IList<Model.EquipmentPool> GetSiegeTroopEquipmentPools(string troopId);
    }
}