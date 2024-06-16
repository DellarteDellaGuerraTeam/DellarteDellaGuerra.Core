using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Equipment.Get.Model;

namespace DellarteDellaGuerra.Infrastructure.Equipment.List.Repositories
{
    public interface ICivilianEquipmentRepository
    {
        IDictionary<string, IList<EquipmentPool>> GetCivilianEquipmentByCharacterAndPool();
    }
}