using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Equipment.List.Model;
using DellarteDellaGuerra.Domain.Equipment.List.Port;

namespace DellarteDellaGuerra.Domain.Equipment.List.Util
{
    public class ListCivilianEquipment : IListCivilianEquipment
    {
        private readonly ICivilianEquipmentRepository _civilianEquipmentRepository;

        public ListCivilianEquipment(ICivilianEquipmentRepository civilianEquipmentRepository)
        {
            _civilianEquipmentRepository = civilianEquipmentRepository;
        }

        public IDictionary<string, IList<EquipmentPool>> ListCivilianEquipmentPools()
        {
            return _civilianEquipmentRepository.GetCivilianEquipmentByCharacterAndPool();
        }
    }
}