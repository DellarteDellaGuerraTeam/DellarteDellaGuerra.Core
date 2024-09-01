using System;
using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Models.EquipmentRosters;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Models.NpcCharacters;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Repositories;
using Equipment = DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Models.NpcCharacters.Equipment;
using EquipmentRoster = DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Models.NpcCharacters.EquipmentRoster;
using EquipmentSet = DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Models.NpcCharacters.EquipmentSet;

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
        IList<EquipmentRoster> equipmentRosters = new List<EquipmentRoster>();
        EquipmentRosters allEquipmentRosters = _equipmentRosterRepository.GetEquipmentRosters();

        foreach (EquipmentSet characterEquipmentSet in npcCharacter.Equipments.EquipmentSet)
        {
            Models.EquipmentRosters.EquipmentRoster equipmentRoster = allEquipmentRosters.EquipmentRoster
                .Find(equipmentRoster => equipmentRoster?.Id?.Equals(characterEquipmentSet.Id) ?? false);

            if (equipmentRoster == null)
                _logger.Warn(
                    $"EquipmentSet with id {characterEquipmentSet.Id} not found for character {npcCharacter.Id}");
            else
                equipmentRoster.EquipmentSet
                    .Where(equipmentSet =>
                        IsSingleFlagTrue(equipmentSet.IsCivilian, equipmentSet.IsSiege) ==
                        IsSingleFlagTrue(characterEquipmentSet.IsCivilian, characterEquipmentSet.IsSiege) ||
                        (AreAllFlagsFalse(equipmentSet.IsCivilian, equipmentSet.IsSiege) &&
                         AreAllFlagsFalse(characterEquipmentSet.IsCivilian, characterEquipmentSet.IsSiege)))
                    .Select(_equipmentSetMapper.MapToEquipmentRoster)
                    .ToList()
                    .ForEach(equipmentRosters.Add);
        }

        IDictionary<string, Equipment> equipmentOverride = npcCharacter.Equipments?.Equipment?
                                                               .Where(equipment => equipment.Slot != null)
                                                               .ToDictionary(
                                                                   equipment => equipment.Slot ?? string.Empty,
                                                                   equipment => equipment)
                                                           ?? new Dictionary<string, Equipment>();

        equipmentRosters = npcCharacter.Equipments?.EquipmentRoster?
            .Concat(equipmentRosters)
            .Select(equipmentRoster => equipmentRoster with
            {
                Equipment = OverrideEquipment(equipmentRoster.Equipment, equipmentOverride)
            }).ToList() ?? new List<EquipmentRoster>();

        return equipmentRosters;
    }

    private List<Equipment> OverrideEquipment(List<Equipment> originalEquipment,
        IDictionary<string, Equipment> equipmentOverride)
    {
        List<Equipment> overriddenEquipment = originalEquipment.Select(equipment => new Equipment
        {
            Id = equipmentOverride.ContainsKey(equipment.Slot)
                ? equipmentOverride[equipment.Slot].Id
                : equipment.Id,
            Slot = equipment.Slot
        }).ToList();

        equipmentOverride.Values
            .Where(overrideItem => overriddenEquipment.TrueForAll(e => e.Slot != overrideItem.Slot))
            .ToList()
            .ForEach(overriddenEquipment.Add);

        return overriddenEquipment;
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