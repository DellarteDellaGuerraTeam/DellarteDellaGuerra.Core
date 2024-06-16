using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Domain.Equipment.Get.Model;
using DellarteDellaGuerra.Domain.Equipment.Get.Port;
using DellarteDellaGuerra.Infrastructure.Cache;
using DellarteDellaGuerra.Infrastructure.Equipment.List.Repositories;

namespace DellarteDellaGuerra.Infrastructure.Equipment.Get
{
    public class TroopCivilianEquipmentProvider : ITroopCivilianEquipmentProvider
    {
        private readonly ILogger _logger;
        private readonly ICivilianEquipmentRepository _civilianEquipmentRepository;
        private readonly ICacheProvider _cacheProvider;
        private readonly string _onGameLoadFinishedCachedObjectId;
        private readonly string _onNewGameCreatedCachedObjectId;

        public TroopCivilianEquipmentProvider(
            ILoggerFactory loggerFactory,
            ICivilianEquipmentRepository civilianEquipmentRepository,
            ICacheProvider cacheProvider)
        {
            _logger = loggerFactory.CreateLogger<TroopCivilianEquipmentProvider>();
            _civilianEquipmentRepository = civilianEquipmentRepository;
            _cacheProvider = cacheProvider;
            _onGameLoadFinishedCachedObjectId =
                _cacheProvider.CacheObjectOnGameLoadFinished(ReadAllTroopEquipmentPools);
            _onNewGameCreatedCachedObjectId = _cacheProvider.CacheObjectOnNewGameCreated(ReadAllTroopEquipmentPools);
        }

        public IList<EquipmentPool> GetCivilianTroopEquipmentPools(string troopId)
        {
            if (string.IsNullOrWhiteSpace(troopId))
            {
                _logger.Debug("The character string id is null or empty.");
                return new List<EquipmentPool>();
            }

            var troopEquipmentPools = GetCachedTroopEquipmentPools();
            if (!troopEquipmentPools.ContainsKey(troopId))
            {
                _logger.Warn($"The character string id {troopId} is not in the civilian equipment pools.");
                return new List<EquipmentPool>();
            }

            return troopEquipmentPools[troopId];
        }

        private IDictionary<string, IList<EquipmentPool>> ReadAllTroopEquipmentPools()
        {
            return _civilianEquipmentRepository.GetCivilianEquipmentByCharacterAndPool();
        }

        private IDictionary<string, IList<EquipmentPool>> GetCachedTroopEquipmentPools()
        {
            return _cacheProvider.GetCachedObject<IDictionary<string, IList<EquipmentPool>>>(
                       _onGameLoadFinishedCachedObjectId) ??
                   _cacheProvider.GetCachedObject<IDictionary<string, IList<EquipmentPool>>>(
                       _onNewGameCreatedCachedObjectId) ?? new Dictionary<string, IList<EquipmentPool>>();
        }
    }
}