using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Models.EquipmentRosters;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Models.NpcCharacters;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Repositories;
using Equipment = DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Models.NpcCharacters.Equipment;
using EquipmentRoster = DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Models.NpcCharacters.EquipmentRoster;

namespace DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Mappers;

public class CharacterEquipmentRostersMapper : ICharacterEquipmentRostersMapper
{
    private readonly IEquipmentRosterRepository _equipmentRosterRepository;
    private readonly IEquipmentRosterMapper _equipmentRosterMapper;
    private readonly ILogger _logger;

    public CharacterEquipmentRostersMapper(IEquipmentRosterRepository equipmentRosterRepository,
        IEquipmentRosterMapper equipmentRosterMapper, ILoggerFactory loggerFactory)
    {
        _equipmentRosterRepository = equipmentRosterRepository;
        _equipmentRosterMapper = equipmentRosterMapper;
        _logger = loggerFactory.CreateLogger<CharacterEquipmentRostersMapper>();
    }

    public IList<EquipmentRoster> Map(NpcCharacter npcCharacter)
    {
        IDictionary<string, Equipment> equipmentOverride =
            npcCharacter.Equipments?.Equipment?
                .Where(equipment => equipment.Slot != null)
                .ToDictionary(equipment => equipment.Slot ?? "", equipment => equipment) ??
            new Dictionary<string, Equipment>();

        var equipmentRosters = npcCharacter.Equipments?.EquipmentRoster.Select(equipmentRoster =>
        {
            return equipmentRoster with
            {
                Equipment = equipmentRoster.Equipment.Select(equipment => new Equipment
                {
                    Id = equipmentOverride.ContainsKey(equipment.Slot)
                        ? equipmentOverride[equipment.Slot].Id
                        : equipment.Id,
                    Slot = equipment.Slot
                }).ToList()
            };
        }).ToList() ?? new List<EquipmentRoster>();

        EquipmentRosters equipmentTemplates = _equipmentRosterRepository.GetEquipmentRosters();

        npcCharacter.Equipments?.EquipmentSet?.ForEach(equipmentSet =>
        {
            Models.EquipmentRosters.EquipmentRoster equipmentTemplate = equipmentTemplates.EquipmentRoster
                .Find(equipmentRoster => equipmentRoster?.Id?.Equals(equipmentSet.Id) ?? false);

            if (equipmentTemplate == null)
                _logger.Warn(
                    $"Equipment template with id {equipmentSet.Id} not found for character {npcCharacter.Id}");
            else
                equipmentTemplate
                    .EquipmentSet
                    .ForEach(referencedEquipmentSet =>
                        equipmentRosters.Add(_equipmentRosterMapper.Map(referencedEquipmentSet)));
        });

        return equipmentRosters;
    }
}