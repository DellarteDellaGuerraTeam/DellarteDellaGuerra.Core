using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Xml.Characters.Repositories.EquipmentPoolSorters;

namespace DellarteDellaGuerra.Xml.Characters.Repositories
{
    public class CharacterSiegeEquipmentRepository
    {
        private readonly ICharacterEquipmentRepository _characterEquipmentRepository;

        public CharacterSiegeEquipmentRepository(ICharacterEquipmentRepository characterEquipmentRepository)
        {
            _characterEquipmentRepository = characterEquipmentRepository;
        }

        public Dictionary<string, IEquipmentPoolSorter> FindSiegeEquipmentByCharacterAndPool()
        {
            return _characterEquipmentRepository
                .FindEquipmentPoolsByCharacter()
                .ToDictionary(
                    characterEquipmentPool => characterEquipmentPool.Key,
                    characterEquipmentPool => new SiegeEquipmentPoolSorter(characterEquipmentPool.Value) as IEquipmentPoolSorter
                );
        }
    }
}
