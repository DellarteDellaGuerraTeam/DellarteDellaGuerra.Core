using System.Collections.Generic;

namespace DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Providers
{
    public interface IEquipmentPoolsRepository
    {
        IDictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>> GetEquipmentPoolsById();
    }
}