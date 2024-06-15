using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using DellarteDellaGuerra.Domain.Equipment.List.Model;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace DellarteDellaGuerra.Equipment.List.Mappers
{
    public class EquipmentPoolMapper
    {
        private readonly MBObjectManager _mbObjectManager;
        
        public EquipmentPoolMapper(MBObjectManager mbObjectManager)
        {
            _mbObjectManager = mbObjectManager;
        }

        public IReadOnlyCollection<MBEquipmentRoster> MapEquipmentPool(IList<EquipmentPool> equipmentPools)
        {
            return equipmentPools
                .Aggregate(new List<MBEquipmentRoster>(), (pools, equipmentPool) =>
            {
                MBEquipmentRoster mbEquipmentLoadout = new MBEquipmentRoster();
                var equipmentNodes = equipmentPool.GetEquipmentLoadouts().Select(equipmentLoadout => equipmentLoadout.GetEquipmentNode());
                
                foreach (var equipmentLoadoutNode in equipmentNodes)
                {
                    XmlNode? node = MapNode(equipmentLoadoutNode);
                    if (node is null)
                    {
                        continue;
                    }
                    AddExplicitEquipmentToPool(node, mbEquipmentLoadout);
                    AddReferencedEquipmentsToPool(node, mbEquipmentLoadout);
                }
                pools.Add(mbEquipmentLoadout);
                return pools;
            });
        }

        private XmlNode? MapNode(XNode node)
        {
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(node.ToString());
            return xmlDocument.DocumentElement;
        }

        private void AddExplicitEquipmentToPool(XmlNode equipmentRosterNode, MBEquipmentRoster equipmentRoster)
        {
            if (equipmentRosterNode.Name.Equals("EquipmentRoster", StringComparison.InvariantCultureIgnoreCase))
            {
                equipmentRoster.Init(_mbObjectManager, equipmentRosterNode);
            }
        }

        private void AddReferencedEquipmentsToPool(XmlNode referencedEquipmentNode, MBEquipmentRoster equipmentRoster)
        {
            if (!referencedEquipmentNode.Name.Equals("EquipmentSet", StringComparison.InvariantCultureIgnoreCase))
            {
                return;
            }

            var id = referencedEquipmentNode.Attributes?["id"]?.Value;
            if (string.IsNullOrWhiteSpace(id))
            {
                return;
            }

            var referencedId = _mbObjectManager.GetObject<MBEquipmentRoster>(id);
            if (referencedId is not null)
            {
                // add all referenced equipments from the EquipmentSet node to the roster
                equipmentRoster.AddEquipmentRoster(referencedId, isCivilian: false);
            }
        }
    }
}