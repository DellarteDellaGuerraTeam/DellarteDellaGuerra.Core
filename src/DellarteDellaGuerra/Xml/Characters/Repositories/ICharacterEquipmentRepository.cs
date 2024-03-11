using System.Collections.Generic;
using DellarteDellaGuerra.Xml.Characters.Repositories.EquipmentPoolSorters;

namespace DellarteDellaGuerra.Xml.Characters.Repositories
{
    public interface ICharacterEquipmentRepository
    {
        IDictionary<string, IEquipmentPoolSorter> FindEquipmentPoolsByCharacter();
    }
}
