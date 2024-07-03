using System.Collections.Generic;

namespace DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Repositories
{
    public interface ISiegeEquipmentRepository
    {
        IDictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>> GetSiegeEquipmentByCharacterAndPool();
    }
}