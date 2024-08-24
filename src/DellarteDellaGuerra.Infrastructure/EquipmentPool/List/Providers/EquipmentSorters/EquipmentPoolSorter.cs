using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using DellarteDellaGuerra.Domain.EquipmentPool.Model;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Models;

namespace DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Providers.EquipmentSorters
{
    public class EquipmentPoolSorter : IEquipmentSorter
    {
        private readonly Dictionary<int, Domain.EquipmentPool.Model.EquipmentPool> _equipmentPools = new ();

        public IList<Domain.EquipmentPool.Model.EquipmentPool> GetEquipmentPools()
        {
            return _equipmentPools.Values.OrderBy(equipmentPool => equipmentPool.GetPoolId()).ToList();
        }

        public void AddEquipmentLoadout<T>(T flagGetter) where T : IPoolFlagGetter
        {
            int poolId = GetPoolId(flagGetter.Pool);
            if (!_equipmentPools.ContainsKey(poolId))
            {
                _equipmentPools.Add(poolId,
                    new Domain.EquipmentPool.Model.EquipmentPool(new List<Equipment>(), poolId));
            }

            var equipment = _equipmentPools[poolId].GetEquipmentLoadouts();
            equipment.Add(new Equipment(GetEquipmentNode(flagGetter)));

            _equipmentPools[poolId] = new Domain.EquipmentPool.Model.EquipmentPool(equipment, poolId);
        }

        private int GetPoolId(string? pool)
        {
            // If the pool attribute is not present, then the pool id is 0.
            int.TryParse(pool, out var poolId);
            return poolId;
        }

        // TODO: remove when XNode is replaced in the domain model
        private XNode GetEquipmentNode<T>(T equipmentRoster)
        {
            // using var stream = new MemoryStream();
            // using var writer = new StreamWriter(stream);
            // var xns = new XmlSerializerNamespaces();
            // xns.Add(string.Empty, string.Empty);
            // var serialiser = new XmlSerializer(typeof(T));
            // serialiser.Serialize(writer, equipmentRoster, xns);
            // // Enables to re-use the stream
            // stream.Position = 0;
            //
            // using var reader = XmlReader.Create(stream);
            // reader.MoveToContent(); // Ensure the reader is in the Interactive state
            // return XNode.ReadFrom(reader);
            var serializer = new XmlSerializer(typeof(T));
            using var stream = new MemoryStream();
            using var writer = new StreamWriter(stream);
            var xns = new XmlSerializerNamespaces();
            xns.Add(string.Empty, string.Empty);
            serializer.Serialize(writer, equipmentRoster, xns);
            stream.Position = 0;

            using var reader = XmlReader.Create(stream);
            reader.MoveToContent();
            return XElement.Load(reader);
        }
    }
}
