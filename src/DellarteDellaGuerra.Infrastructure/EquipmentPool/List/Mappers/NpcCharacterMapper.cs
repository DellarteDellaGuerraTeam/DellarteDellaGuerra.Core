using System;
using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Models.EquipmentRosters;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Models.NpcCharacters;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Repositories;
using Equipment = DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Models.NpcCharacters.Equipment;
using EquipmentRoster = DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Models.NpcCharacters.EquipmentRoster;

namespace DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Mappers;

public class NpcCharacterMapper : INpcCharacterMapper
{
    private readonly IEquipmentRosterRepository _equipmentRosterRepository;
    private readonly IEquipmentSetMapper _equipmentSetMapper;
    private readonly ILogger _logger;

    public NpcCharacterMapper(IEquipmentRosterRepository equipmentRosterRepository,
        IEquipmentSetMapper equipmentSetMapper, ILoggerFactory loggerFactory)
    {
        _equipmentRosterRepository = equipmentRosterRepository;
        _equipmentSetMapper = equipmentSetMapper;
        _logger = loggerFactory.CreateLogger<NpcCharacterMapper>();
    }

    public IList<EquipmentRoster> MapToEquipmentRosters(NpcCharacter npcCharacter)
    {
        IDictionary<string, Equipment> equipmentOverride = npcCharacter.Equipments?.Equipment?
                                                               .Where(equipment => equipment.Slot != null)
                                                               .ToDictionary(
                                                                   equipment => equipment.Slot ?? string.Empty,
                                                                   equipment => equipment)
                                                           ?? new Dictionary<string, Equipment>();

        IList<EquipmentRoster> equipmentRosters = npcCharacter.Equipments?.EquipmentRoster
            .Select(equipmentRoster => equipmentRoster with
            {
                Equipment = equipmentRoster.Equipment
                    .Where(equipment => equipment.Slot != null)
                    .Select(equipment => new Equipment
                {
                    Id = equipmentOverride.ContainsKey(equipment.Slot)
                        ? equipmentOverride[equipment.Slot].Id
                        : equipment.Id,
                    Slot = equipment.Slot
                }).ToList()
            }).ToList() ?? new List<EquipmentRoster>();

        EquipmentRosters allEquipmentRosters = _equipmentRosterRepository.GetEquipmentRosters();

        if (npcCharacter.Equipments?.EquipmentSet != null)
            foreach (var characterEquipmentSet in npcCharacter.Equipments.EquipmentSet)
            {
                Models.EquipmentRosters.EquipmentRoster equipmentRoster = allEquipmentRosters.EquipmentRoster
                    .Find(equipmentRoster => equipmentRoster?.Id?.Equals(characterEquipmentSet.Id) ?? false);

                if (equipmentRoster == null)
                {
                    _logger.Warn(
                        $"Equipment template with id {characterEquipmentSet.Id} not found for character {npcCharacter.Id}");
                }
                else
                {
                    var matchedEquipmentSets = equipmentRoster.EquipmentSet
                        .Where(equipmentSet =>
                            IsSingleFlagTrue(equipmentSet.IsCivilian, equipmentSet.IsSiege) ==
                            IsSingleFlagTrue(characterEquipmentSet.IsCivilian, characterEquipmentSet.IsSiege) ||
                            (AreAllFlagsFalse(equipmentSet.IsCivilian, equipmentSet.IsSiege) &&
                             AreAllFlagsFalse(characterEquipmentSet.IsCivilian, characterEquipmentSet.IsSiege)))
                        .Select(_equipmentSetMapper.MapToEquipmentRoster)
                        .ToList();

                    matchedEquipmentSets.ForEach(equipmentRosters.Add);
                }
            }

        return equipmentRosters;
    }

    private static bool IsSingleFlagTrue(string? isCivilian, string? isSiege)
    {
        return bool.TrueString.Equals(isCivilian, StringComparison.OrdinalIgnoreCase) ^
               bool.TrueString.Equals(isSiege, StringComparison.OrdinalIgnoreCase);
    }

    private static bool AreAllFlagsFalse(string? isCivilian, string? isSiege)
    {
        return !bool.TrueString.Equals(isCivilian, StringComparison.OrdinalIgnoreCase) &&
               !bool.TrueString.Equals(isSiege, StringComparison.OrdinalIgnoreCase);
    }
}