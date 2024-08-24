using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Models.EquipmentRosters;

namespace DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Repositories
{
    public interface IEquipmentRosterRepository
    {
        EquipmentRosters GetEquipmentRosters();
    }
}