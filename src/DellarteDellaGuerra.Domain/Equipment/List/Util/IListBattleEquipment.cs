using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Equipment.List.Model;

namespace DellarteDellaGuerra.Domain.Equipment.List.Util
{
    public interface IListBattleEquipment
    {
        IDictionary<string, IList<EquipmentPool>> ListBattleEquipmentPools();
    }
}