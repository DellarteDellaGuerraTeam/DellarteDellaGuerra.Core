using System.Collections.Generic;
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

        public GetEquipmentPool(IEncounterTypeProvider encounterTypeProvider,
            ITroopBattleEquipmentProvider troopBattleEquipmentProvider,
            ITroopSiegeEquipmentProvider troopSiegeEquipmentProvider,
            ITroopCivilianEquipmentProvider troopCivilianEquipmentProvider,
            IEquipmentPoolPicker equipmentPoolPicker)
        {
            _encounterTypeProvider = encounterTypeProvider;
            _troopBattleEquipmentProvider = troopBattleEquipmentProvider;
            _troopSiegeEquipmentProvider = troopSiegeEquipmentProvider;
            _troopCivilianEquipmentProvider = troopCivilianEquipmentProvider;
            _equipmentPoolPicker = equipmentPoolPicker;
        }

        public Model.EquipmentPool GetTroopEquipmentPool(string troopId)
        {
            return _equipmentPoolPicker.PickEquipmentPool(GetEquipmentPools(troopId));
        }

        private IList<Model.EquipmentPool> GetEquipmentPools(string troopId)
        {
            switch (_encounterTypeProvider.GetEncounterType())
            {
                case EncounterType.Battle:
                    return _troopBattleEquipmentProvider.GetBattleTroopEquipmentPools(troopId);
                case EncounterType.Siege:
                    return _troopSiegeEquipmentProvider.GetSiegeTroopEquipmentPools(troopId);
                case EncounterType.Civilian:
                    return _troopCivilianEquipmentProvider.GetCivilianTroopEquipmentPools(troopId);
                default:
                    return _troopBattleEquipmentProvider.GetBattleTroopEquipmentPools(troopId);
            }
        }
    }
}