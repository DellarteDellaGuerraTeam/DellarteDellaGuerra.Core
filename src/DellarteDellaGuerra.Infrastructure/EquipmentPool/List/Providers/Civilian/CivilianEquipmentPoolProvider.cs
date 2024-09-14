using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Infrastructure.Caching;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Providers.EquipmentSorters;

namespace DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Providers.Civilian
{
    public class CivilianEquipmentPoolProvider(ILoggerFactory loggerFactory,
        ICacheProvider cacheProvider,
        params IEquipmentPoolsRepository[] equipmentRepositories) : ICivilianEquipmentPoolProvider
    {
        private readonly ILogger _logger = loggerFactory.CreateLogger<CivilianEquipmentPoolProvider>();

        private string? _onSessionLaunchedCachedObjectId;
        
        public IDictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>>
            GetCivilianEquipmentByCharacterAndPool()
        {
            if (_onSessionLaunchedCachedObjectId is not null)
            {
                IDictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>>? cachedNpcCharacters =
                    cacheProvider.GetCachedObject<IDictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>>>(
                        _onSessionLaunchedCachedObjectId);
                if (cachedNpcCharacters is not null) return cachedNpcCharacters;

                _logger.Error("The cached equipment pools are null.");
                return new Dictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>>();
            }

            var civilianEquipmentPoolsByCharacter = equipmentRepositories
                .SelectMany(repo => repo.GetEquipmentPoolsById())
                .GroupBy(pools => pools.Key)
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

                        return new CivilianEquipmentSorter(equipmentPools).GetEquipmentPools();
                    });

            _onSessionLaunchedCachedObjectId = cacheProvider.CacheObject(civilianEquipmentPoolsByCharacter);
            cacheProvider.InvalidateCache(_onSessionLaunchedCachedObjectId, CampaignEvent.OnAfterSessionLaunched);

            return civilianEquipmentPoolsByCharacter;
        }
    }
}