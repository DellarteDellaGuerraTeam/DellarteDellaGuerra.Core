using System.Collections.Generic;

namespace DellarteDellaGuerra.Domain.EquipmentPool.Port
{
    public interface ITroopBattleEquipmentProvider
    {
        IList<Model.EquipmentPool> GetBattleTroopEquipmentPools(string equipmentId);
    }
}