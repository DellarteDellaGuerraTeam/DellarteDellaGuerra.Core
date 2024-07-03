using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Repositories.EquipmentSorters;

namespace DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Repositories
{
    public class CivilianEquipmentRepository : ICivilianEquipmentRepository
    {
        private readonly IEquipmentRepository _equipmentRepository;

        public CivilianEquipmentRepository(IEquipmentRepository equipmentRepository)
        {
            _equipmentRepository = equipmentRepository;
        }

        public IDictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>> GetCivilianEquipmentByCharacterAndPool()
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
