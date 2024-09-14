using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Domain.EquipmentPool.Model;
using DellarteDellaGuerra.Infrastructure.Caching;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Providers.Civilian;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Providers.Siege;

namespace DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Providers.Battle
{
    public class BattleEquipmentPoolProvider : IBattleEquipmentPoolProvider
    {
        private readonly ILogger _logger;
        private readonly ICacheProvider _cacheProvider;
        private readonly IEquipmentPoolsRepository[] _equipmentPoolRepositories;
        private readonly ISiegeEquipmentPoolProvider _siegeEquipmentPoolProvider;
        private readonly ICivilianEquipmentPoolProvider _civilianEquipmentPoolProvider;

        private string? _onSessionLaunchedCachedObjectId;
        
        public BattleEquipmentPoolProvider(
            ILoggerFactory loggerFactory,
            ICacheProvider cacheProvider,
            ISiegeEquipmentPoolProvider siegeEquipmentPoolProvider,
            ICivilianEquipmentPoolProvider civilianEquipmentPoolProvider,
            params IEquipmentPoolsRepository[] equipmentPoolRepositories)
        {
            _logger = loggerFactory.CreateLogger<BattleEquipmentPoolProvider>();
            _cacheProvider = cacheProvider;
            _equipmentPoolRepositories = equipmentPoolRepositories;
            _siegeEquipmentPoolProvider = siegeEquipmentPoolProvider;
            _civilianEquipmentPoolProvider = civilianEquipmentPoolProvider;
        }

        public IDictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>> GetBattleEquipmentByCharacterAndPool()
        {
            if (_onSessionLaunchedCachedObjectId is not null)
            {
                IDictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>>? cachedNpcCharacters =
                    _cacheProvider
                        .GetCachedObject<IDictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>>>(
                            _onSessionLaunchedCachedObjectId);
                if (cachedNpcCharacters is not null) return cachedNpcCharacters;

                _logger.Error("The cached equipment pools are null.");
                return new Dictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>>();
            }
            
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

            _onSessionLaunchedCachedObjectId =
                _cacheProvider.CacheObject(equipmentPoolsByCharacterWithoutCivilianAndSiege);
            _cacheProvider.InvalidateCache(_onSessionLaunchedCachedObjectId, CampaignEvent.OnAfterSessionLaunched);

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