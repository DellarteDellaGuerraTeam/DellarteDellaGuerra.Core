using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.SpawnEquipmentPools.ListEquipment.Model;
using DellarteDellaGuerra.Domain.SpawnEquipmentPools.ListEquipment.Port;
using DellarteDellaGuerra.Infrastructure.SpawnEquipmentPools.ListEquipment.Repositories.EquipmentSorters;

namespace DellarteDellaGuerra.Infrastructure.SpawnEquipmentPools.ListEquipment.Repositories
{
    public class CharacterSiegeEquipmentRepository : ICharacterSiegeEquipmentRepository
    {
        private readonly ICharacterEquipmentRepository _characterEquipmentRepository;

        public CharacterSiegeEquipmentRepository(ICharacterEquipmentRepository characterEquipmentRepository)
        {
            _characterEquipmentRepository = characterEquipmentRepository;
        }

        public IDictionary<string, IList<EquipmentPool>> GetSiegeEquipmentByCharacterAndPool()
        {
            return _characterEquipmentRepository
                .GetEquipmentPoolsByCharacter()
                .ToDictionary(
                    characterEquipmentPools => characterEquipmentPools.Key,
                    characterEquipmentPools => new SiegeEquipmentSorter(characterEquipmentPools.Value).GetEquipmentPools()
                );
        }
    }
}
