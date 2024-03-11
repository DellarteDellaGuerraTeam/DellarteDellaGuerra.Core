using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;

namespace DellarteDellaGuerra.Xml.Characters.Repositories.EquipmentPoolSorters
{
    public class EquipmentPoolSorter : IEquipmentPoolSorter
    {
        private readonly Dictionary<int, List<XNode>> _equipmentPools = new();

        public IReadOnlyCollection<IReadOnlyCollection<XNode>> GetEquipmentPools()
        {
            return _equipmentPools.Values;
        }

        public void AddEquipmentLoadout(XNode node)
        {
            int poolId = GetPoolId(node);
            if (!_equipmentPools.ContainsKey(poolId))
            {
                _equipmentPools.Add(poolId, new List<XNode>());
            }
            _equipmentPools[poolId].Add(node);            
        }

        private int GetPoolId(XNode node)
        {
            if (node.NodeType is not XmlNodeType.Element)
                return -1;
            var element = (XElement) node;
            var pool = element.Attribute("pool")?.Value?.Trim();
            // default pool is 0 if not specified
            int.TryParse(pool, out var poolId);
            return poolId;
        }
    }
}
