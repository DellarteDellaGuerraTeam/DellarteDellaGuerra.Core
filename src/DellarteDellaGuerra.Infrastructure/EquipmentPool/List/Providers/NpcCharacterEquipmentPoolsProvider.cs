using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Mappers;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Models.NpcCharacters;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Providers.EquipmentSorters;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Repositories;

namespace DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Providers;

public class NpcCharacterEquipmentPoolsProvider
(INpcCharacterRepository npcCharacterRepository,
    INpcCharacterMapper npcCharacterMapper) : IEquipmentPoolsRepository
{
    public IDictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>> GetEquipmentPoolsById()
    {
        NpcCharacters npcCharacters = npcCharacterRepository.GetNpcCharacters();

        return npcCharacters.NpcCharacter.Aggregate(
            new Dictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>>(),
            (characterEquipmentPools, npcCharacter) =>
            {
                if (npcCharacter.Id != null)
                    characterEquipmentPools[npcCharacter.Id] = npcCharacterMapper.MapToEquipmentRosters(npcCharacter)
                        .Aggregate(
                            new EquipmentPoolSorter(), (equipmentPoolSorter, characterEquipmentRosters) =>
                            {
                                equipmentPoolSorter.AddEquipmentLoadout(characterEquipmentRosters);
                                return equipmentPoolSorter;
                            }).GetEquipmentPools();

                return characterEquipmentPools;
            });
    }
}