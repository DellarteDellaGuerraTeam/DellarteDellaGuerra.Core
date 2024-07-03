using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;

namespace DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Repositories.EquipmentSorters
{
    public class CivilianEquipmentSorter : IEquipmentSorter
    {
        private readonly IList<Domain.EquipmentPool.Model.EquipmentPool> _equipmentPools;
        
        public CivilianEquipmentSorter(IList<Domain.EquipmentPool.Model.EquipmentPool> equipmentPools)
        {
            _equipmentPools = equipmentPools;
        }

        private bool MatchesCriteria(XNode node)
        {
            // If the civilian attribute is true, then it is a civilian equipment
            bool.TryParse((string) node.XPathEvaluate("string(@civilian)"), out var isSiege);
            return isSiege;
        }

        public IList<Domain.EquipmentPool.Model.EquipmentPool> GetEquipmentPools()
        {
            return _equipmentPools
                .Select(equipmentPool =>
                {
                    var equipment = equipmentPool.GetEquipmentLoadouts()
                        .Where(equipmentLoadout => MatchesCriteria(equipmentLoadout.GetEquipmentNode()))
                        .ToList();
                    return new Domain.EquipmentPool.Model.EquipmentPool(equipment, equipmentPool.GetPoolId());        
                })
                .Where(equipmentPool => !equipmentPool.IsEmpty())
                .ToList();
        }
    }
}
