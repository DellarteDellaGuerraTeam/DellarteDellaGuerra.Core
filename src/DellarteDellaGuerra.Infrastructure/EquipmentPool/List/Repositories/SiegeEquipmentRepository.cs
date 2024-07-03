using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Repositories.EquipmentSorters;

namespace DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Repositories
{
    public class SiegeEquipmentRepository : ISiegeEquipmentRepository
    {
        private readonly IEquipmentRepository _equipmentRepository;

        public SiegeEquipmentRepository(IEquipmentRepository equipmentRepository)
        {
            _equipmentRepository = equipmentRepository;
        }

        public IDictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>> GetSiegeEquipmentByCharacterAndPool()
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
