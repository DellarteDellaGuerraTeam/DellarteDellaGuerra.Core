using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using DellarteDellaGuerra.Domain.SpawnEquipmentPools.ListEquipment.Model;

namespace DellarteDellaGuerra.Infrastructure.SpawnEquipmentPools.ListEquipment.Repositories.EquipmentSorters
{
    public class CivilianEquipmentSorter : IEquipmentSorter
    {
        private readonly IList<EquipmentPool> _equipmentPools;
        
        public CivilianEquipmentSorter(IList<EquipmentPool> equipmentPools)
        {
            _equipmentPools = equipmentPools;
        }

        private bool MatchesCriteria(XNode node)
        {
            // If the civilian attribute is true, then it is a civilian equipment
            bool.TryParse((string) node.XPathEvaluate("string(@civilian)"), out var isSiege);
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
