using System;
using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Models.NpcCharacters;
using EquipmentSet = DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Models.EquipmentRosters.EquipmentSet;

namespace DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Mappers;

public class EquipmentRosterMapper : IEquipmentRosterMapper
{
    public EquipmentRoster Map(EquipmentSet equipmentSet)
    {
        if (equipmentSet == null) throw new ArgumentNullException(nameof(equipmentSet));

        return new EquipmentRoster
        {
            IsCivilian = equipmentSet.IsCivilian,
            IsSiege = equipmentSet.IsSiege,
            Pool = equipmentSet.Pool,
            Equipment = equipmentSet.Equipment?.Select(equipment => new Equipment
            {
                Id = equipment.Id,
                Slot = equipment.Slot
            }).ToList() ?? new List<Equipment>()
        };
    }
}