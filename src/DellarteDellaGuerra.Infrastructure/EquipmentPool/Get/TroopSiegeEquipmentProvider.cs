using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Domain.EquipmentPool.Port;
using DellarteDellaGuerra.Infrastructure.Caching;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Providers.Siege;

namespace DellarteDellaGuerra.Infrastructure.EquipmentPool.Get
{
    public class TroopSiegeEquipmentProvider : ITroopSiegeEquipmentProvider
    {
        private readonly ILogger _logger;
        private readonly ISiegeEquipmentPoolProvider _siegeEquipmentPoolProvider;
        private readonly ICacheProvider _cacheProvider;
        private readonly string _onSessionLaunchedCachedObjectId;

        public TroopSiegeEquipmentProvider(
            ILoggerFactory loggerFactory,
            ISiegeEquipmentPoolProvider siegeEquipmentPoolProvider,
            ICacheProvider cacheProvider)
        {
            _logger = loggerFactory.CreateLogger<TroopSiegeEquipmentProvider>();
            _siegeEquipmentPoolProvider = siegeEquipmentPoolProvider;
            _cacheProvider = cacheProvider;
            _onSessionLaunchedCachedObjectId =
                _cacheProvider.CacheObject(ReadAllTroopEquipmentPools, CampaignEvent.OnSessionLaunched);
        }

        public IList<Domain.EquipmentPool.Model.EquipmentPool> GetSiegeTroopEquipmentPools(string equipmentId)
        {
            if (string.IsNullOrWhiteSpace(equipmentId))
            {
                _logger.Debug("The equipment id is null or empty.");
                return new List<Domain.EquipmentPool.Model.EquipmentPool>();
            }

            var troopEquipmentPools = GetCachedTroopEquipmentPools();
            if (!troopEquipmentPools.ContainsKey(equipmentId))
            {
                _logger.Warn($"The equipment id {equipmentId} is not in the battle equipment pools.");
                return new List<Domain.EquipmentPool.Model.EquipmentPool>();
            }

            return troopEquipmentPools[equipmentId];
        }

        private IDictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>> ReadAllTroopEquipmentPools()
        {
            return _siegeEquipmentPoolProvider.GetSiegeEquipmentByCharacterAndPool();
        }

        private IDictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>> GetCachedTroopEquipmentPools()
        {
            return _cacheProvider.GetCachedObject<IDictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>>>(
                _onSessionLaunchedCachedObjectId) ?? new Dictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>>();
        }
    }
}