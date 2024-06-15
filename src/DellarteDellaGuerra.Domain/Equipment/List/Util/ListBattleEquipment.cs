using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.Equipment.List.Model;
using DellarteDellaGuerra.Domain.Equipment.List.Port;

namespace DellarteDellaGuerra.Domain.Equipment.List.Util
{
    public class ListBattleEquipment : IListBattleEquipment
    {
        private readonly IEquipmentRepository _equipmentRepository;
        private readonly ISiegeEquipmentRepository _siegeEquipmentRepository;
        private readonly ICivilianEquipmentRepository _civilianEquipmentRepository;

        public ListBattleEquipment(
            IEquipmentRepository equipmentRepository,
            ISiegeEquipmentRepository siegeEquipmentRepository,
            ICivilianEquipmentRepository civilianEquipmentRepository)
        {
            _equipmentRepository = equipmentRepository;
            _siegeEquipmentRepository = siegeEquipmentRepository;
            _civilianEquipmentRepository = civilianEquipmentRepository;
        }

        public IDictionary<string, IList<EquipmentPool>> ListBattleEquipmentPools()
        {
            var siegeEquipmentPoolsByCharacter = _siegeEquipmentRepository.GetSiegeEquipmentByCharacterAndPool();
            var civilianEquipmentPoolsByCharacter =
                _civilianEquipmentRepository.GetCivilianEquipmentByCharacterAndPool();
            var equipmentPoolsByCharacter = _equipmentRepository.GetEquipmentPoolsByCharacter();

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
            var equipmentToRemove = equipmentPool.GetEquipmentLoadouts();

            IList<Model.Equipment> equipmentPoolReferenceFiltered = equipmentPoolReference.GetEquipmentLoadouts()
                .Where(equipment => !equipmentToRemove.Contains(equipment)).ToList();

            int poolId = equipmentPoolReference.GetPoolId();

            return new EquipmentPool(equipmentPoolReferenceFiltered, poolId);
        }
    }
}
