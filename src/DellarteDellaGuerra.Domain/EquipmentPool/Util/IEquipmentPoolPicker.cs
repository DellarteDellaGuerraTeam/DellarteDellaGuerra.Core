using System.Collections.Generic;

namespace DellarteDellaGuerra.Domain.EquipmentPool.Util
{
    public interface IEquipmentPoolPicker
    {
        Model.EquipmentPool PickEquipmentPool(IList<Model.EquipmentPool> equipmentPools);
    }
}