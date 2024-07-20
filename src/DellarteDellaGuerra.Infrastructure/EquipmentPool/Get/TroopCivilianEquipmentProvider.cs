using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Domain.EquipmentPool.Port;
using DellarteDellaGuerra.Infrastructure.Caching;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Providers.Civilian;

namespace DellarteDellaGuerra.Infrastructure.EquipmentPool.Get
{
    public class TroopCivilianEquipmentProvider : ITroopCivilianEquipmentProvider
    {
        private readonly ILogger _logger;
        private readonly ICivilianEquipmentPoolProvider _civilianEquipmentPoolProvider;
        private readonly ICacheProvider _cacheProvider;
        private readonly string _onSessionLaunchedCachedObjectId;

        public TroopCivilianEquipmentProvider(
            ILoggerFactory loggerFactory,
            ICivilianEquipmentPoolProvider civilianEquipmentPoolProvider,
            ICacheProvider cacheProvider)
        {
            _logger = loggerFactory.CreateLogger<TroopCivilianEquipmentProvider>();
            _civilianEquipmentPoolProvider = civilianEquipmentPoolProvider;
            _cacheProvider = cacheProvider;
            _onSessionLaunchedCachedObjectId =
                _cacheProvider.CacheObject(ReadAllTroopEquipmentPools, CampaignEvent.OnSessionLaunched);
        }

        public IList<Domain.EquipmentPool.Model.EquipmentPool> GetCivilianTroopEquipmentPools(string equipmentId)
        {
            if (string.IsNullOrWhiteSpace(equipmentId))
            {
                _logger.Debug("The equipment id is null or empty.");
                return new List<Domain.EquipmentPool.Model.EquipmentPool>();
            }

            var troopEquipmentPools = GetCachedTroopEquipmentPools();
            if (!troopEquipmentPools.ContainsKey(equipmentId))
            {
                _logger.Warn($"The equipment id {equipmentId} is not in the civilian equipment pools.");
                return new List<Domain.EquipmentPool.Model.EquipmentPool>();
            }

            return troopEquipmentPools[equipmentId];
        }

        private IDictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>> ReadAllTroopEquipmentPools()
        {
            return _civilianEquipmentPoolProvider.GetCivilianEquipmentByCharacterAndPool();
        }

        private IDictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>> GetCachedTroopEquipmentPools()
        {
            return _cacheProvider.GetCachedObject<IDictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>>>(
                _onSessionLaunchedCachedObjectId) ?? new Dictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>>();
        }
    }
}