using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using DellarteDellaGuerra.Xml.Characters.Repositories.EquipmentPoolSorters;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace DellarteDellaGuerra.Xml.Characters.Mappers
{
    public class EquipmentPoolMapper
    {
        private readonly MBObjectManager _mbObjectManager;
        
        public EquipmentPoolMapper(MBObjectManager mbObjectManager)
        {
            _mbObjectManager = mbObjectManager;
        }

        public IReadOnlyCollection<MBEquipmentRoster> MapEquipmentPool(IEquipmentPoolSorter equipmentPools)
        {
            return equipmentPools
                .GetEquipmentPools()
                .Aggregate(new List<MBEquipmentRoster>(), (pools, equipmentPool) =>
            {
                MBEquipmentRoster equipmentLoadout = new MBEquipmentRoster();

                foreach (var equipmentLoadoutNode in equipmentPool)
                {
                    XmlNode node = MapNode(equipmentLoadoutNode);
                    AddExplicitEquipmentToPool(node, equipmentLoadout);
                    AddReferencedEquipmentsToPool(node, equipmentLoadout);
                }
                pools.Add(equipmentLoadout);
                return pools;
            });
        }

        private XmlNode MapNode(XNode node)
        {
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(node.ToString());
            return xmlDocument;
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