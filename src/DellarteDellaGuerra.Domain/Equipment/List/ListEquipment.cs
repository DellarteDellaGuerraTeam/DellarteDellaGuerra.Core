using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Equipment.List.Model;
using DellarteDellaGuerra.Domain.Equipment.List.Port;
using DellarteDellaGuerra.Domain.Equipment.List.Util;

namespace DellarteDellaGuerra.Domain.Equipment.List
{
    public class ListEquipment
    {
        private readonly IEncounterTypeProvider _encounterTypeProvider;
        private readonly IListBattleEquipment _listBattleEquipment;
        private readonly IListCivilianEquipment _listCivilianEquipment;
        private readonly IListSiegeEquipment _listSiegeEquipment;

        public ListEquipment(IEncounterTypeProvider encounterTypeProvider, IListBattleEquipment listBattleEquipment,
            IListCivilianEquipment listCivilianEquipment, IListSiegeEquipment listSiegeEquipment)
        {
            _encounterTypeProvider = encounterTypeProvider;
            _listBattleEquipment = listBattleEquipment;
            _listCivilianEquipment = listCivilianEquipment;
            _listSiegeEquipment = listSiegeEquipment;
        }

        public IDictionary<string, IList<EquipmentPool>> ListEquipmentPools()
        {
            switch (_encounterTypeProvider.GetEncounterType())
            {
                case EncounterType.Battle:
                    return _listBattleEquipment.ListBattleEquipmentPools();
                case EncounterType.Siege:
                    return _listSiegeEquipment.ListSiegeEquipmentPools();
                case EncounterType.Civilian:
                    return _listCivilianEquipment.ListCivilianEquipmentPools();
                default:
                    return _listBattleEquipment.ListBattleEquipmentPools();
            }
        }
    }
}