using System.Collections.Generic;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Models.NpcCharacters;

namespace DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Mappers;

public interface ICharacterEquipmentRostersMapper
{
    IList<EquipmentRoster> Map(NpcCharacter npcCharacter);
}