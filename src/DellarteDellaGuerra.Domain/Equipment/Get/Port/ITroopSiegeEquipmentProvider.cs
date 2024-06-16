using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Equipment.Get.Model;

namespace DellarteDellaGuerra.Domain.Equipment.Get.Port
{
    public interface ITroopSiegeEquipmentProvider
    {
        IList<EquipmentPool> GetSiegeTroopEquipmentPools(string troopId);
    }
}