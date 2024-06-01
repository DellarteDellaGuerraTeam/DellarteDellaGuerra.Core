using System.Collections.Generic;
using DellarteDellaGuerra.Domain.SpawnEquipmentPools.ListEquipment.Model;

namespace DellarteDellaGuerra.Domain.SpawnEquipmentPools.ListEquipment.Port
{
    public interface ICharacterSiegeEquipmentRepository
    {
        IDictionary<string, IList<EquipmentPool>> GetSiegeEquipmentByCharacterAndPool();
    }
}
