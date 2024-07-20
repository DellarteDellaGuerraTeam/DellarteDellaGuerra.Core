using System.Collections.Generic;

namespace DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Repositories
{
    public interface IEquipmentPoolRepository
    {
        IDictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>> GetEquipmentPoolsById();
    }
}