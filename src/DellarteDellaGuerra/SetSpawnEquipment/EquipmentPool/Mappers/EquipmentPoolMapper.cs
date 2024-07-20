﻿using System;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
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
            string equipmentId)
        {
            var mbEquipmentLoadouts = new MBEquipmentRoster();
            var equipmentNodes = equipmentPool.GetEquipmentLoadouts()
                .Select(equipmentLoadout => equipmentLoadout.GetEquipmentNode());

            equipmentNodes = XDocument
                .Parse(
                    "<EquipmentRoster><equipment slot=\"Item0\" id=\"Item.lance_b\"/><equipment slot=\"Item1\" id=\"Item.ddg_mace_frenchmace\"/><equipment slot=\"Item2\" id=\"Item.wallace_A462\"/><equipment slot=\"Item3\" id=\"Item.inv_shield\"/><equipment slot=\"Body\" id=\"Item.simple_livery_coat_over_mail\"/><equipment slot=\"Leg\" id=\"Item.hosen_with_boots_a\"/><equipment slot=\"Head\" id=\"Item.open_decorated_helmet_with_orle\"/><equipment slot=\"Horse\" id=\"Item.t2_vlandia_horse\"/><equipment slot=\"HorseHarness\" id=\"Item.harness_mixed_a\"/></EquipmentRoster>")
                .XPathSelectElements("./EquipmentRoster");
            
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

            // var nativeEquipmentLoadout = FindMatchingEquipment(equipmentId, equipmentLoadout);

            // if (nativeEquipmentLoadout is null)
            //     return;

            var equipment = (MBList<Equipment>)_mbEquipmentRosterEquipmentsField.GetValue(equipmentRoster);
            equipment.Add(equipmentLoadout);
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

        private Equipment? FindMatchingEquipment(string equipmentId, Equipment equipment)
        {
            var nativeEquipmentPool = _mbObjectManager.GetObject<MBEquipmentRoster>(equipmentId);

            return nativeEquipmentPool.AllEquipments.Find(nativeEquipmentLoadout =>
                nativeEquipmentLoadout.IsEquipmentEqualTo(equipment));
        }
    }
}