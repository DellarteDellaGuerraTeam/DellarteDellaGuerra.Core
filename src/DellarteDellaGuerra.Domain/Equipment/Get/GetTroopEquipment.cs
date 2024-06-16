using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Equipment.Get.Model;
using DellarteDellaGuerra.Domain.Equipment.Get.Port;

namespace DellarteDellaGuerra.Domain.Equipment.Get
{
    public class GetTroopEquipment : IGetTroopEquipment
    {
        private readonly IEncounterTypeProvider _encounterTypeProvider;
        private readonly ITroopBattleEquipmentProvider _troopBattleEquipmentProvider;
        private readonly ITroopSiegeEquipmentProvider _troopSiegeEquipmentProvider;
        private readonly ITroopCivilianEquipmentProvider _troopCivilianEquipmentProvider;

        public GetTroopEquipment(IEncounterTypeProvider encounterTypeProvider,
            ITroopBattleEquipmentProvider troopBattleEquipmentProvider,
            ITroopSiegeEquipmentProvider troopSiegeEquipmentProvider,
            ITroopCivilianEquipmentProvider troopCivilianEquipmentProvider)
        {
            _encounterTypeProvider = encounterTypeProvider;
            _troopBattleEquipmentProvider = troopBattleEquipmentProvider;
            _troopSiegeEquipmentProvider = troopSiegeEquipmentProvider;
            _troopCivilianEquipmentProvider = troopCivilianEquipmentProvider;
        }

        public IList<EquipmentPool> GetEquipmentPools(string troopId)
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