using System.Collections.Generic;

namespace DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Providers.EquipmentSorters
{
    public interface IEquipmentSorter
    {
        IList<Domain.EquipmentPool.Model.EquipmentPool> GetEquipmentPools();
    }
}
