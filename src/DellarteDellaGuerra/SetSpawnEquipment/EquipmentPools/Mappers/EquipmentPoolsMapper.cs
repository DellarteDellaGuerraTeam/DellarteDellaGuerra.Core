using System;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Domain.EquipmentPool.Model;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;
using Equipment = TaleWorlds.Core.Equipment;

namespace DellarteDellaGuerra.SetSpawnEquipment.EquipmentPools.Mappers
{
    public class EquipmentPoolsMapper
    {
        private readonly MBObjectManager _mbObjectManager;
        private readonly ILogger _logger;

        private readonly FieldInfo _mbEquipmentRosterEquipmentsField =
            typeof(MBEquipmentRoster).GetField("_equipments", BindingFlags.NonPublic | BindingFlags.Instance);

        public EquipmentPoolsMapper(MBObjectManager mbObjectManager, ILoggerFactory loggerFactory)
        {
            _mbObjectManager = mbObjectManager;
            _logger = loggerFactory.CreateLogger<EquipmentPoolsMapper>();
        }

        public MBEquipmentRoster MapEquipmentPool(EquipmentPool equipmentPool,
            string equipmentId)
        {
            var mbEquipmentLoadouts = new MBEquipmentRoster();
            var equipmentNodes = equipmentPool.GetEquipmentLoadouts()
                .Select(equipmentLoadout => equipmentLoadout.GetEquipmentNode());

            foreach (var equipmentLoadoutNode in equipmentNodes)
            {
                var node = MapNode(equipmentLoadoutNode);
                if (node is null) continue;

                if (node.Name.Equals("EquipmentRoster", StringComparison.InvariantCultureIgnoreCase))
                    AddEquipmentNodeToEquipmentRoster(node, mbEquipmentLoadouts, equipmentId);
                else if (node.Name.Equals("EquipmentSet", StringComparison.InvariantCultureIgnoreCase))
                    AddReferencedEquipmentsToPool(node, mbEquipmentLoadouts, equipmentId);
            }

            return mbEquipmentLoadouts;
        }

        private XmlNode? MapNode(XNode node)
        {
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(node.ToString());
            return xmlDocument.DocumentElement;
        }

        private void AddEquipmentNodeToEquipmentRoster(XmlNode equipmentRosterNode, MBEquipmentRoster equipmentRoster,
            string equipmentId)
        {
            var equipmentLoadout =
                new Equipment(bool.Parse(equipmentRosterNode.Attributes?["civilian"]?.Value ?? "false"));
            equipmentLoadout.Deserialize(_mbObjectManager, equipmentRosterNode);

            var nativeEquipmentLoadout = FindMatchingEquipment(equipmentId, equipmentLoadout);

            if (nativeEquipmentLoadout is null)
            {
                _logger.Error($"Could not find {equipmentLoadout} among native '{equipmentId}' equipment roster");
                return;
            }

            var equipment = (MBList<Equipment>)_mbEquipmentRosterEquipmentsField.GetValue(equipmentRoster);
            equipment.Add(nativeEquipmentLoadout);
        }

        private Equipment? FindMatchingEquipment(string equipmentId, Equipment equipment)
        {
            var nativeEquipmentPool = _mbObjectManager.GetObject<MBEquipmentRoster>(equipmentId);

            if (nativeEquipmentPool is null) return null;

            // TODO: handle use case when nativeEquipmentPool is not found
            return nativeEquipmentPool.AllEquipments.Find(nativeEquipmentLoadout =>
                nativeEquipmentLoadout.IsEquipmentEqualTo(equipment));
        }
        
        private void AddReferencedEquipmentsToPool(XmlNode referencedEquipmentNode, MBEquipmentRoster equipmentRoster,
            string equipmentId)
        {
            var id = referencedEquipmentNode.Attributes?["id"]?.Value;
            if (string.IsNullOrWhiteSpace(id))
            {
                AddEquipmentNodeToEquipmentRoster(referencedEquipmentNode, equipmentRoster, equipmentId);
                return;
            }

            var referencedId = _mbObjectManager.GetObject<MBEquipmentRoster>(id);
            if (referencedId is null) return;

            bool.TryParse(referencedEquipmentNode.Attributes?["civilian"]?.Value, out var isCivilian);
            // add all referenced equipments from the EquipmentSet node to the roster
            equipmentRoster.AddEquipmentRoster(referencedId, isCivilian);
        }
    }
}