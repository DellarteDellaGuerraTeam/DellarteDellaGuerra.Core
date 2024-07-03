using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Domain.EquipmentPool.Port;
using DellarteDellaGuerra.Infrastructure.Cache;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Repositories;

namespace DellarteDellaGuerra.Infrastructure.EquipmentPool.Get
{
    public class TroopSiegeEquipmentProvider : ITroopSiegeEquipmentProvider
    {
        private readonly ILogger _logger;
        private readonly ISiegeEquipmentRepository _siegeEquipmentRepository;
        private readonly ICacheProvider _cacheProvider;
        private readonly string _onSessionLaunchedCachedObjectId;

        public TroopSiegeEquipmentProvider(
            ILoggerFactory loggerFactory,
            ISiegeEquipmentRepository siegeEquipmentRepository,
            ICacheProvider cacheProvider)
        {
            _logger = loggerFactory.CreateLogger<TroopSiegeEquipmentProvider>();
            _siegeEquipmentRepository = siegeEquipmentRepository;
            _cacheProvider = cacheProvider;
            _onSessionLaunchedCachedObjectId =
                _cacheProvider.CacheObject(ReadAllTroopEquipmentPools, CachedEvent.OnSessionLaunched);
        }

        public IList<Domain.EquipmentPool.Model.EquipmentPool> GetSiegeTroopEquipmentPools(string troopId)
        {
            if (string.IsNullOrWhiteSpace(troopId))
            {
                _logger.Debug("The character string id is null or empty.");
                return new List<Domain.EquipmentPool.Model.EquipmentPool>();
            }

            var troopEquipmentPools = GetCachedTroopEquipmentPools();
            if (!troopEquipmentPools.ContainsKey(troopId))
            {
                _logger.Warn($"The character string id {troopId} is not in the battle equipment pools.");
                return new List<Domain.EquipmentPool.Model.EquipmentPool>();
            }

            return troopEquipmentPools[troopId];
        }

        private IDictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>> ReadAllTroopEquipmentPools()
        {
            return _siegeEquipmentRepository.GetSiegeEquipmentByCharacterAndPool();
        }

        private IDictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>> GetCachedTroopEquipmentPools()
        {
            return _cacheProvider.GetCachedObject<IDictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>>>(
                _onSessionLaunchedCachedObjectId) ?? new Dictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>>();
        }
    }
}