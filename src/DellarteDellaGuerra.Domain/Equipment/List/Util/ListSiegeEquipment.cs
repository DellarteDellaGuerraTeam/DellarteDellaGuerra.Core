using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Equipment.List.Model;
using DellarteDellaGuerra.Domain.Equipment.List.Port;

namespace DellarteDellaGuerra.Domain.Equipment.List.Util
{
    public class ListSiegeEquipment : IListSiegeEquipment
    {
        private readonly ISiegeEquipmentRepository _siegeEquipmentRepository;

        public ListSiegeEquipment(ISiegeEquipmentRepository siegeEquipmentRepository)
        {
            _siegeEquipmentRepository = siegeEquipmentRepository;
        }

        public IDictionary<string, IList<EquipmentPool>> ListSiegeEquipmentPools()
        {
            return _siegeEquipmentRepository.GetSiegeEquipmentByCharacterAndPool();
        }
    }
}