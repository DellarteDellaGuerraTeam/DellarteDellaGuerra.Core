using System.Collections.Generic;
using DellarteDellaGuerra.Domain.SpawnEquipmentPools.ListEquipment.Model;

namespace DellarteDellaGuerra.Domain.SpawnEquipmentPools.ListEquipment.Port
{
    public interface ICharacterCivilianEquipmentRepository
    {
        IDictionary<string, IList<EquipmentPool>> GetCivilianEquipmentByCharacterAndPool();
    }
}
