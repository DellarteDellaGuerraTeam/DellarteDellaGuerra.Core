using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Equipment.Get.Model;

namespace DellarteDellaGuerra.Infrastructure.Equipment.List.Repositories.EquipmentSorters
{
    public interface IEquipmentSorter
    {
        IList<EquipmentPool> GetEquipmentPools();
    }
}
