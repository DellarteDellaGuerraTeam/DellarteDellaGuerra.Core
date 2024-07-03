using System.Collections.Generic;

namespace DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Repositories
{
    public interface IBattleEquipmentRepository
    {
        IDictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>> GetBattleEquipmentByCharacterAndPool();
    }
}