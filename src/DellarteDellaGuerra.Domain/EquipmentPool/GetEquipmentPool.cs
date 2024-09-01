using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Domain.EquipmentPool.Model;
using DellarteDellaGuerra.Domain.EquipmentPool.Port;
using DellarteDellaGuerra.Domain.EquipmentPool.Util;

namespace DellarteDellaGuerra.Domain.EquipmentPool
{
    public class GetEquipmentPool : IGetEquipmentPool
    {
        private readonly IEncounterTypeProvider _encounterTypeProvider;
        private readonly ITroopBattleEquipmentProvider _troopBattleEquipmentProvider;
        private readonly ITroopSiegeEquipmentProvider _troopSiegeEquipmentProvider;
        private readonly ITroopCivilianEquipmentProvider _troopCivilianEquipmentProvider;
        private readonly IEquipmentPoolPicker _equipmentPoolPicker;
        private readonly ILogger _logger;

        public GetEquipmentPool(IEncounterTypeProvider encounterTypeProvider,
            ITroopBattleEquipmentProvider troopBattleEquipmentProvider,
            ITroopSiegeEquipmentProvider troopSiegeEquipmentProvider,
            ITroopCivilianEquipmentProvider troopCivilianEquipmentProvider,
            IEquipmentPoolPicker equipmentPoolPicker,
            ILoggerFactory loggerFactory)
        {
            _encounterTypeProvider = encounterTypeProvider;
            _troopBattleEquipmentProvider = troopBattleEquipmentProvider;
            _troopSiegeEquipmentProvider = troopSiegeEquipmentProvider;
            _troopCivilianEquipmentProvider = troopCivilianEquipmentProvider;
            _equipmentPoolPicker = equipmentPoolPicker;
            _logger = loggerFactory.CreateLogger<GetEquipmentPool>();
        }

        public Model.EquipmentPool GetTroopEquipmentPool(string troopId)
        {
            return _equipmentPoolPicker.PickEquipmentPool(GetEquipmentPools(troopId));
        }

        private IList<Model.EquipmentPool> GetEquipmentPools(string troopId)
        {
            _logger.Debug(
                $"Getting equipment pools from {_encounterTypeProvider.GetEncounterType()} for troop '{troopId}'.");
            IList<Model.EquipmentPool> equipmentPools = _encounterTypeProvider.GetEncounterType() switch
            {
                EncounterType.Battle => _troopBattleEquipmentProvider.GetBattleTroopEquipmentPools(troopId),
                EncounterType.Siege => _troopSiegeEquipmentProvider.GetSiegeTroopEquipmentPools(troopId),
                EncounterType.Civilian => _troopCivilianEquipmentProvider.GetCivilianTroopEquipmentPools(troopId),
                _ => _troopBattleEquipmentProvider.GetBattleTroopEquipmentPools(troopId)
            };

            if (!equipmentPools.SelectMany(e => e.GetEquipmentLoadouts()).Any())
                _logger.Warn(
                    $"No equipment found for troop '{troopId}' in {_encounterTypeProvider.GetEncounterType()} encounter.");

            return equipmentPools;
        }
    }
}