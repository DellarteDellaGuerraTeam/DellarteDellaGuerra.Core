﻿using System.Collections.Generic;
using System.Linq;
using System.Xml.XPath;
using DellarteDellaGuerra.Domain.Equipment.Get.Model;
using DellarteDellaGuerra.Infrastructure.Equipment.List.Repositories.EquipmentSorters;
using DellarteDellaGuerra.Infrastructure.Equipment.List.Utils;
using SafeFluentXPath.Implementation.Api;

namespace DellarteDellaGuerra.Infrastructure.Equipment.List.Repositories
{
    public class EquipmentRepository : IEquipmentRepository
    {
        private readonly INpcCharacterXmlProcessor _npcCharacterXmlProcessor;

        public EquipmentRepository(INpcCharacterXmlProcessor npcCharacterXmlProcessor)
        {
            _npcCharacterXmlProcessor = npcCharacterXmlProcessor;
        }

        public IDictionary<string, IList<EquipmentPool>> GetEquipmentPoolsByCharacter()
        {
            return Extensions.XPathSelectElements(_npcCharacterXmlProcessor
                    .GetMergedXmlCharacterNodes(), CharacterEquipmentXPath())
                .Aggregate(new Dictionary<string, IEquipmentSorter>(),
                    (characterEquipmentPools, equipmentNode) =>
                    {
                        var characterId = (string)equipmentNode.XPathEvaluate("string(../../@id)");

                        if (!characterEquipmentPools.ContainsKey(characterId))
                        {
                            characterEquipmentPools[characterId] = new EquipmentPoolSorter();
                        }

                        ((EquipmentPoolSorter)characterEquipmentPools[characterId]).AddEquipmentLoadout(equipmentNode);
                        return characterEquipmentPools;
                    })
                .ToDictionary(characterEquipmentPools => characterEquipmentPools.Key,
                    characterEquipmentPools =>
                    {
                        return characterEquipmentPools.Value.GetEquipmentPools();
                    });
        }

        private string CharacterEquipmentXPath()
        {
            return new XPathBuilder()
                .Element("NPCCharacters")
                .ChildElement("NPCCharacter")
                .ChildElement("Equipments")
                .ChildElementsAtSameLevel(
                    "EquipmentRoster",
                    "equipmentRoster",
                    "EquipmentSet",
                    "equipmentSet"
                )
                .Build();
        }
    }
}