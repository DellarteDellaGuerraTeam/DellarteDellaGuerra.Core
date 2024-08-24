using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Models.EquipmentRosters;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Providers.EquipmentSorters;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Repositories;

namespace DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Providers;

public class EquipmentRosterEquipmentPoolsProvider
    (IEquipmentRosterRepository equipmentRosterRepository) : IEquipmentPoolsRepository
{
    public IDictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>> GetEquipmentPoolsById()
    {
        EquipmentRosters equipmentRosters = equipmentRosterRepository.GetEquipmentRosters();

        return equipmentRosters.EquipmentRoster.Aggregate(new Dictionary<string, IEquipmentSorter>(),
                (characterEquipmentPools, equipmentRoster) =>
                {
                    var equipmentRosterId = equipmentRoster.Id;

                    if (!characterEquipmentPools.ContainsKey(equipmentRosterId))
                        characterEquipmentPools[equipmentRosterId] = new EquipmentPoolSorter();

                    equipmentRoster.EquipmentSet?.ForEach(roster =>
                    {
                        ((EquipmentPoolSorter)characterEquipmentPools[equipmentRosterId]).AddEquipmentLoadout(
                            roster);
                    });

                    return characterEquipmentPools;
                })
            .ToDictionary(characterEquipmentPools => characterEquipmentPools.Key,
                characterEquipmentPools => characterEquipmentPools.Value.GetEquipmentPools());
    }
}