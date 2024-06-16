using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.Equipment.Get.Model;
using DellarteDellaGuerra.Infrastructure.Equipment.List.Repositories.EquipmentSorters;

namespace DellarteDellaGuerra.Infrastructure.Equipment.List.Repositories
{
    public class CivilianEquipmentRepository : ICivilianEquipmentRepository
    {
        private readonly IEquipmentRepository _equipmentRepository;

        public CivilianEquipmentRepository(IEquipmentRepository equipmentRepository)
        {
            _equipmentRepository = equipmentRepository;
        }

        public IDictionary<string, IList<EquipmentPool>> GetCivilianEquipmentByCharacterAndPool()
        {
            return _equipmentRepository
                .GetEquipmentPoolsByCharacter()
                .ToDictionary(
                    characterEquipmentPools => characterEquipmentPools.Key,
                    characterEquipmentPools => new CivilianEquipmentSorter(characterEquipmentPools.Value).GetEquipmentPools()
                );
        }
    }
}
