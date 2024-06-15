using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.Equipment.List.Model;
using DellarteDellaGuerra.Domain.Equipment.List.Port;
using DellarteDellaGuerra.Infrastructure.Equipment.List.Repositories.EquipmentSorters;

namespace DellarteDellaGuerra.Infrastructure.Equipment.List.Repositories
{
    public class SiegeEquipmentRepository : ISiegeEquipmentRepository
    {
        private readonly IEquipmentRepository _equipmentRepository;

        public SiegeEquipmentRepository(IEquipmentRepository equipmentRepository)
        {
            _equipmentRepository = equipmentRepository;
        }

        public IDictionary<string, IList<EquipmentPool>> GetSiegeEquipmentByCharacterAndPool()
        {
            return _equipmentRepository
                .GetEquipmentPoolsByCharacter()
                .ToDictionary(
                    characterEquipmentPools => characterEquipmentPools.Key,
                    characterEquipmentPools => new SiegeEquipmentSorter(characterEquipmentPools.Value).GetEquipmentPools()
                );
        }
    }
}
