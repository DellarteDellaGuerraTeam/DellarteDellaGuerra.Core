using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using DellarteDellaGuerra.Domain.Equipment.Get.Model;

namespace DellarteDellaGuerra.Infrastructure.Equipment.List.Repositories.EquipmentSorters
{
    public class SiegeEquipmentSorter : IEquipmentSorter
    {
        private readonly IList<EquipmentPool> _equipmentPools;

        public SiegeEquipmentSorter(IList<EquipmentPool> equipmentPools)
        {
            _equipmentPools = equipmentPools;
        }
        
        private bool MatchesCriteria(XNode node)
        {
            // If the siege attribute is true, then it is a siege equipment.
            bool.TryParse((string) node.XPathEvaluate("string(@siege)"), out var isSiege);
            return isSiege;
        }

        public IList<EquipmentPool> GetEquipmentPools()
        {
            return _equipmentPools
                .Select(equipmentPool =>
                {
                    var equipment = equipmentPool.GetEquipmentLoadouts()
                        .Where(equipmentLoadout => MatchesCriteria(equipmentLoadout.GetEquipmentNode()))
                        .ToList();
                    return new EquipmentPool(equipment, equipmentPool.GetPoolId());        
                })
                .Where(equipmentPool => !equipmentPool.IsEmpty())
                .ToList();
        }
    }
}
