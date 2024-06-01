using System.Collections.Generic;
using DellarteDellaGuerra.Domain.SpawnEquipmentPools.ListEquipment.Model;

namespace DellarteDellaGuerra.Infrastructure.SpawnEquipmentPools.ListEquipment.Repositories.EquipmentSorters
{
    public interface IEquipmentSorter
    {
        IList<EquipmentPool> GetEquipmentPools();
    }
}
