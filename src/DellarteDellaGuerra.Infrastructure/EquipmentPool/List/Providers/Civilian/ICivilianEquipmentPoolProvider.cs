using System.Collections.Generic;

namespace DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Providers.Civilian
{
    public interface ICivilianEquipmentPoolProvider
    {
        IDictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>> GetCivilianEquipmentByCharacterAndPool();
    }
}