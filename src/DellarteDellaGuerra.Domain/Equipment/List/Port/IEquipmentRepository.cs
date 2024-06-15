using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Equipment.List.Model;

namespace DellarteDellaGuerra.Domain.Equipment.List.Port
{
    public interface IEquipmentRepository
    {
        IDictionary<string, IList<EquipmentPool>> GetEquipmentPoolsByCharacter();
    }
}