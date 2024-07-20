using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Domain.EquipmentPool.Model;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Providers.Civilian;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Providers.Siege;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Repositories;

namespace DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Providers.Battle
{
    public class BattleEquipmentPoolProvider : IBattleEquipmentPoolProvider
    {
        private readonly ILogger _logger;
        private readonly IEquipmentPoolRepository[] _equipmentPoolRepositories;
        private readonly ISiegeEquipmentPoolProvider _siegeEquipmentPoolProvider;
        private readonly ICivilianEquipmentPoolProvider _civilianEquipmentPoolProvider;

        public BattleEquipmentPoolProvider(
            ILoggerFactory loggerFactory,
            ISiegeEquipmentPoolProvider siegeEquipmentPoolProvider,
            ICivilianEquipmentPoolProvider civilianEquipmentPoolProvider,
            params IEquipmentPoolRepository[] equipmentPoolRepositories)
        {
            _logger = loggerFactory.CreateLogger<BattleEquipmentPoolProvider>();
            _equipmentPoolRepositories = equipmentPoolRepositories;
            _siegeEquipmentPoolProvider = siegeEquipmentPoolProvider;
            _civilianEquipmentPoolProvider = civilianEquipmentPoolProvider;
        }

        public IDictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>> GetBattleEquipmentByCharacterAndPool()
        {
            var siegeEquipmentPoolsByCharacter = _siegeEquipmentPoolProvider.GetSiegeEquipmentByCharacterAndPool();
            var civilianEquipmentPoolsByCharacter =
                _civilianEquipmentPoolProvider.GetCivilianEquipmentByCharacterAndPool();
            var equipmentPoolsByCharacter = _equipmentPoolRepositories.SelectMany(repo => repo.GetEquipmentPoolsById())
                .GroupBy(equipmentPool => equipmentPool.Key)
                .ToDictionary(
                    group => group.Key,
                    group =>
                    {
                        IList<Domain.EquipmentPool.Model.EquipmentPool> equipmentPools;
                        if (group.Count() > 1)
                        {
                            equipmentPools = group.First().Value;
                            _logger.Warn(
                                $"'{group.Key}' is defined in multiple xml files. Only the first equipment list will be used.");
                        }
                        else
                        {
                            equipmentPools = group.SelectMany(pool => pool.Value).ToList();
                        }

                        return equipmentPools;
                    });

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