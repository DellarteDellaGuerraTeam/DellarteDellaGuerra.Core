using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using DellarteDellaGuerra.Domain.Equipment.Get.Model;

namespace DellarteDellaGuerra.Infrastructure.Equipment.List.Repositories.EquipmentSorters
{
    public class EquipmentPoolSorter : IEquipmentSorter
    {
        private readonly Dictionary<int, EquipmentPool> _equipmentPools = new ();

        public IList<EquipmentPool> GetEquipmentPools()
        {
            return _equipmentPools.Values.ToList();
        }

        public void AddEquipmentLoadout(XNode node)
        {
            int poolId = GetPoolId(node);
            if (!_equipmentPools.ContainsKey(poolId))
            {
                _equipmentPools.Add(poolId,
                    new EquipmentPool(new List<Domain.Equipment.Get.Model.Equipment>(), poolId));
            }

            var equipment = _equipmentPools[poolId].GetEquipmentLoadouts();
            equipment.Add(new Domain.Equipment.Get.Model.Equipment(node));

            _equipmentPools[poolId] = new EquipmentPool(equipment, poolId);
        }

        private int GetPoolId(XNode node)
        {
            if (node.NodeType is not XmlNodeType.Element)
                return -1;
            var element = (XElement) node;
            var pool = element.Attribute("pool")?.Value.Trim();
            // default pool is 0 if not specified
            int.TryParse(pool, out var poolId);
            return poolId;
        }
    }
}
