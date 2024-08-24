using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Models.NpcCharacters;
using EquipmentSet = DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Models.EquipmentRosters.EquipmentSet;

namespace DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Mappers;

public interface IEquipmentRosterMapper
{
    EquipmentRoster Map(EquipmentSet equipmentSet);
}