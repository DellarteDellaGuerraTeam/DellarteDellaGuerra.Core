using System.Collections.Generic;
using System.Linq;
using System.Xml.XPath;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Providers.EquipmentSorters;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Utils;
using SafeFluentXPath.Implementation.Api;

namespace DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Repositories
{
    public class CharacterEquipmentPoolRepository : IEquipmentPoolRepository
    {
        internal const string NpcCharacterRootTag = "NPCCharacters";

        private readonly IXmlProcessor _xmlProcessor;

        public CharacterEquipmentPoolRepository(IXmlProcessor xmlProcessor)
        {
            _xmlProcessor = xmlProcessor;
        }

        public IDictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>> GetEquipmentPoolsById()
        {
            return _xmlProcessor
                .GetXmlNodes(NpcCharacterRootTag).XPathSelectElements(CharacterEquipmentXPath())
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
                    characterEquipmentPools => characterEquipmentPools.Value.GetEquipmentPools());
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