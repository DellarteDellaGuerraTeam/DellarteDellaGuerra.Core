using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Domain.EquipmentPool.Port;
using DellarteDellaGuerra.Infrastructure.Cache;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Repositories;

namespace DellarteDellaGuerra.Infrastructure.EquipmentPool.Get
{
    /**
     * <summary>
     *     This class provides a way to store the equipment pools for troops.
     * </summary>
     * <remarks>
     *     It reads the characters from the xml and applies the xsl transformations to get the equipment pools for each troop.
     *     <br />
     *     <br />
     *     An equipment pool is a collection of equipment defined by its pool attribute.
     *     The pool attribute is optional and defaults to 0 if not specified.
     *     <br />
     *     <br />
     *     EquipmentLoadout loadouts with the same pool attribute are grouped together to form an equipment pool.
     * </remarks>
     */
    public class TroopBattleEquipmentProvider : ITroopBattleEquipmentProvider
    {
        private readonly ILogger _logger;
        private readonly IBattleEquipmentRepository _battleEquipmentRepository;
        private readonly ICacheProvider _cacheProvider;
        private readonly string _onSessionLaunchedCachedObjectId;

        public TroopBattleEquipmentProvider(
            ILoggerFactory loggerFactory,
            IBattleEquipmentRepository battleEquipmentRepository,
            ICacheProvider cacheProvider)
        {
            _logger = loggerFactory.CreateLogger<TroopBattleEquipmentProvider>();
            _battleEquipmentRepository = battleEquipmentRepository;
            _cacheProvider = cacheProvider;
            _onSessionLaunchedCachedObjectId =
                _cacheProvider.CacheObject(ReadAllTroopEquipmentPools, CachedEvent.OnSessionLaunched);
        }

        /**
         * <summary>
         *     Get the equipment pools for a troop.
         * </summary>
         * <param name="troopId">The troop character</param>
         * <returns>The equipment pools for the troop</returns>
         * <remarks>
         *     If the character is a hero or a non combatant character, then an empty list is returned.
         *     If the character is null or the character string id is null or empty, then an empty list is returned.
         * </remarks>
         */
        public IList<Domain.EquipmentPool.Model.EquipmentPool> GetBattleTroopEquipmentPools(string troopId)
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
            return _battleEquipmentRepository.GetBattleEquipmentByCharacterAndPool();
        }

        private IDictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>> GetCachedTroopEquipmentPools()
        {
            return _cacheProvider.GetCachedObject<IDictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>>>(
                _onSessionLaunchedCachedObjectId) ?? new Dictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>>();
        }
    }
}