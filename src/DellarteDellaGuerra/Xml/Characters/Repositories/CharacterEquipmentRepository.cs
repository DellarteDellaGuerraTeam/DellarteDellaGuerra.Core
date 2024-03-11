using System.Collections.Generic;
using System.Linq;
using System.Xml.XPath;
using DellarteDellaGuerra.Xml.Characters.Repositories.EquipmentPoolSorters;
using SafeFluentXPath.Implementation.Api;

namespace DellarteDellaGuerra.Xml.Characters.Repositories
{
    public class CharacterEquipmentRepository : ICharacterEquipmentRepository
    {
        private readonly IXmlProcessor _xmlProcessor;

        public CharacterEquipmentRepository(IXmlProcessor xmlProcessor)
        {
            _xmlProcessor = xmlProcessor;
        }

        public IDictionary<string, IEquipmentPoolSorter> FindEquipmentPoolsByCharacter()
        {
            return _xmlProcessor
                .GetMergedXmlCharacterNodes()
                .XPathSelectElements(CharacterEquipmentXPath())
                .Aggregate(new Dictionary<string, IEquipmentPoolSorter>(),
                    (characterEquipmentPools, equipmentNode) =>
                    {
                        var characterId = (string)equipmentNode.XPathEvaluate("string(../../@id)");

                        if (!characterEquipmentPools.ContainsKey(characterId))
                        {
                            characterEquipmentPools[characterId] = new EquipmentPoolSorter();
                        }
                        ((EquipmentPoolSorter) characterEquipmentPools[characterId]).AddEquipmentLoadout(equipmentNode);
                        return characterEquipmentPools;
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