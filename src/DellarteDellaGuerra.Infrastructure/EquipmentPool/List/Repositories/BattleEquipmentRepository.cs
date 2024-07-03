using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.EquipmentPool.Model;

namespace DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Repositories
{
    public class BattleEquipmentRepository : IBattleEquipmentRepository
    {
        private readonly IEquipmentRepository _equipmentRepository;
        private readonly ISiegeEquipmentRepository _siegeEquipmentRepository;
        private readonly ICivilianEquipmentRepository _civilianEquipmentRepository;

        public BattleEquipmentRepository(
            IEquipmentRepository equipmentRepository,
            ISiegeEquipmentRepository siegeEquipmentRepository,
            ICivilianEquipmentRepository civilianEquipmentRepository)
        {
            _equipmentRepository = equipmentRepository;
            _siegeEquipmentRepository = siegeEquipmentRepository;
            _civilianEquipmentRepository = civilianEquipmentRepository;
        }

        public IDictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>> GetBattleEquipmentByCharacterAndPool()
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

        private IDictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>> FilterOutMatchingEquipment(
            IDictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>> equipmentPoolsReference,
            IDictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>> equipmentPools)
        {
            return equipmentPoolsReference.ToDictionary(
                characterEquipmentPools => characterEquipmentPools.Key,
                characterEquipmentPools =>
                {
                    equipmentPools.TryGetValue(characterEquipmentPools.Key,
                        out var equipmentPoolsByCharacter);

                    if (equipmentPoolsByCharacter is null) return characterEquipmentPools.Value;

                    return FilterOutMatchingEquipment(characterEquipmentPools.Value, equipmentPoolsByCharacter);
                }
            );
        }

        private IList<Domain.EquipmentPool.Model.EquipmentPool> FilterOutMatchingEquipment(
            IList<Domain.EquipmentPool.Model.EquipmentPool> equipmentPoolsReference,
            IList<Domain.EquipmentPool.Model.EquipmentPool> equipmentPools)
        {
            var equipmentByPoolId = equipmentPools.ToDictionary(equipmentPool => equipmentPool.GetPoolId(),
                equipmentPool => equipmentPool);

            return equipmentPoolsReference
                .Select(equipmentPoolReference =>
                {
                    if (equipmentByPoolId.ContainsKey(equipmentPoolReference.GetPoolId()))
                        return FilterOutMatchingEquipment(equipmentPoolReference,
                            equipmentByPoolId[equipmentPoolReference.GetPoolId()]);

                    return equipmentPoolReference;
                }).Where(equipmentPool => !equipmentPool.IsEmpty()).ToList();
        }

        private Domain.EquipmentPool.Model.EquipmentPool FilterOutMatchingEquipment(Domain.EquipmentPool.Model.EquipmentPool equipmentPoolReference,
            Domain.EquipmentPool.Model.EquipmentPool equipmentPool)
        {
            var equipmentToRemove = equipmentPool.GetEquipmentLoadouts();

            IList<Equipment> equipmentPoolReferenceFiltered = equipmentPoolReference
                .GetEquipmentLoadouts()
                .Where(equipment => !equipmentToRemove.Contains(equipment)).ToList();

            var poolId = equipmentPoolReference.GetPoolId();

            return new Domain.EquipmentPool.Model.EquipmentPool(equipmentPoolReferenceFiltered, poolId);
        }
    }
}