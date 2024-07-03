using System;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace DellarteDellaGuerra.SetSpawnEquipment.EquipmentPool.Mappers
{
    public class EquipmentPoolMapper
    {
        private readonly MBObjectManager _mbObjectManager;

        private readonly FieldInfo _mbEquipmentRosterEquipmentsField =
            typeof(MBEquipmentRoster).GetField("_equipments", BindingFlags.NonPublic | BindingFlags.Instance);
        
        public EquipmentPoolMapper(MBObjectManager mbObjectManager)
        {
            _mbObjectManager = mbObjectManager;
        }

        public MBEquipmentRoster MapEquipmentPool(Domain.EquipmentPool.Model.EquipmentPool equipmentPool,
            string troopId)
        {
            var mbEquipmentLoadouts = new MBEquipmentRoster();
            var equipmentNodes = equipmentPool.GetEquipmentLoadouts()
                .Select(equipmentLoadout => equipmentLoadout.GetEquipmentNode());

            foreach (var equipmentLoadoutNode in equipmentNodes)
            {
                var node = MapNode(equipmentLoadoutNode);
                if (node is null) continue;

                AddExplicitEquipmentToPool(node, mbEquipmentLoadouts, troopId);
                AddReferencedEquipmentsToPool(node, mbEquipmentLoadouts);
            }

            return mbEquipmentLoadouts;
        }

        private XmlNode? MapNode(XNode node)
        {
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(node.ToString());
            return xmlDocument.DocumentElement;
        }

        private void AddExplicitEquipmentToPool(XmlNode equipmentRosterNode, MBEquipmentRoster equipmentRoster,
            string troopId)
        {
            if (equipmentRosterNode.Name.Equals("EquipmentRoster", StringComparison.InvariantCultureIgnoreCase))
            {
                var equipmentLoadout = new Equipment(
                    bool.Parse(equipmentRosterNode.Attributes?["civilian"]?.Value ?? "false"));
                equipmentLoadout.Deserialize(_mbObjectManager, equipmentRosterNode);

                var nativeEquipmentPool = _mbObjectManager.GetObject<MBEquipmentRoster>(troopId);
                var nativeEquipmentLoadout = nativeEquipmentPool.AllEquipments.Find(nativeEquipmentLoadout =>
                    nativeEquipmentLoadout.IsEquipmentEqualTo(equipmentLoadout));

                if (nativeEquipmentLoadout is not null)
                {
                    var equipment =
                        (MBList<Equipment>)_mbEquipmentRosterEquipmentsField.GetValue(
                            equipmentRoster);
                    equipment.Add(nativeEquipmentLoadout);
                }
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
                bool.TryParse(referencedEquipmentNode.Attributes?["civilian"]?.Value, out var isCivilian);
                // add all referenced equipments from the EquipmentSet node to the roster
                equipmentRoster.AddEquipmentRoster(referencedId, isCivilian);
            }
        }
    }
}