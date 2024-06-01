using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.SpawnEquipmentPools.ListEquipment.Model;
using DellarteDellaGuerra.Domain.SpawnEquipmentPools.ListEquipment.Port;

namespace DellarteDellaGuerra.Domain.SpawnEquipmentPools.ListEquipment
{
    public class ListBattleEquipment
    {
        private readonly ICharacterEquipmentRepository _characterEquipmentRepository;
        private readonly ICharacterSiegeEquipmentRepository _characterSiegeEquipmentRepository;
        private readonly ICharacterCivilianEquipmentRepository _characterCivilianEquipmentRepository;

        public ListBattleEquipment(
            ICharacterEquipmentRepository characterEquipmentRepository,
            ICharacterSiegeEquipmentRepository characterSiegeEquipmentRepository,
            ICharacterCivilianEquipmentRepository characterCivilianEquipmentRepository)
        {
            _characterEquipmentRepository = characterEquipmentRepository;
            _characterSiegeEquipmentRepository = characterSiegeEquipmentRepository;
            _characterCivilianEquipmentRepository = characterCivilianEquipmentRepository;
        }

        public IDictionary<string, IList<EquipmentPool>> ListEquipment()
        {
            var siegeEquipmentPoolsByCharacter = _characterSiegeEquipmentRepository.GetSiegeEquipmentByCharacterAndPool();
            var civilianEquipmentPoolsByCharacter = _characterCivilianEquipmentRepository.GetCivilianEquipmentByCharacterAndPool();
            var equipmentPoolsByCharacter = _characterEquipmentRepository.GetEquipmentPoolsByCharacter();

            var equipmentPoolsByCharacterWithoutSiege = 
                FilterOutMatchingEquipment(equipmentPoolsByCharacter, siegeEquipmentPoolsByCharacter);

            var equipmentPoolsByCharacterWithoutCivilianAndSiege = 
                FilterOutMatchingEquipment(equipmentPoolsByCharacterWithoutSiege, civilianEquipmentPoolsByCharacter);

            return equipmentPoolsByCharacterWithoutCivilianAndSiege;
        }

        private IDictionary<string, IList<EquipmentPool>> FilterOutMatchingEquipment(
            IDictionary<string, IList<EquipmentPool>> equipmentPoolsReference,
            IDictionary<string, IList<EquipmentPool>> equipmentPools)
        {
            return equipmentPoolsReference.ToDictionary(
                characterEquipmentPools => characterEquipmentPools.Key,
                characterEquipmentPools =>
                {
                    equipmentPools.TryGetValue(characterEquipmentPools.Key,
                        out IList<EquipmentPool> equipmentPoolsByCharacter);

                    if (equipmentPoolsByCharacter is null)
                    {
                        return characterEquipmentPools.Value;
                    }

                    return FilterOutMatchingEquipment(characterEquipmentPools.Value, equipmentPoolsByCharacter);
                }
            );
        }

        private IList<EquipmentPool> FilterOutMatchingEquipment(
            IList<EquipmentPool> equipmentPoolsReference,
            IList<EquipmentPool> equipmentPools)
        {
            var equipmentByPoolId = equipmentPools.ToDictionary(equipmentPool => equipmentPool.GetPoolId(),
                equipmentPool => equipmentPool);

            return equipmentPoolsReference
                .Select(equipmentPoolReference =>
                {
                    if (equipmentByPoolId.ContainsKey(equipmentPoolReference.GetPoolId()))
                    {
                        return FilterOutMatchingEquipment(equipmentPoolReference, equipmentByPoolId[equipmentPoolReference.GetPoolId()]);
                    }

                    return equipmentPoolReference;
                }).
                Where(equipmentPool => !equipmentPool.IsEmpty()).
                ToList();
        }

        private EquipmentPool FilterOutMatchingEquipment(EquipmentPool equipmentPoolReference, EquipmentPool equipmentPool)
        {
            IList<Equipment> equipmentToRemove = equipmentPool.GetEquipmentLoadouts();

            IList<Equipment> equipmentPoolReferenceFiltered = equipmentPoolReference.GetEquipmentLoadouts()
                .Where(equipment => !equipmentToRemove.Contains(equipment)).ToList();

            int poolId = equipmentPoolReference.GetPoolId();

            return new EquipmentPool(equipmentPoolReferenceFiltered, poolId);
        }
    }
}
