using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Xml.Characters.Repositories.EquipmentPoolSorters;

namespace DellarteDellaGuerra.Xml.Characters.Repositories
{
    public class CharacterCivilianEquipmentRepository
    {
        private readonly ICharacterEquipmentRepository _characterEquipmentRepository;

        public CharacterCivilianEquipmentRepository(ICharacterEquipmentRepository characterEquipmentRepository)
        {
            _characterEquipmentRepository = characterEquipmentRepository;
        }

        public Dictionary<string, IEquipmentPoolSorter> FindCivilianEquipmentByCharacterAndPool()
        {
            return _characterEquipmentRepository
                .FindEquipmentPoolsByCharacter()
                .ToDictionary(
                    characterEquipmentPool => characterEquipmentPool.Key,
                    characterEquipmentPool => new CivilianEquipmentPoolSorter(characterEquipmentPool.Value) as IEquipmentPoolSorter
                );
        }
    }
}
