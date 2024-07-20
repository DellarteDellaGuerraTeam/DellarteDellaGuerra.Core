using System.Collections.Generic;
using System.Linq;
using System.Xml.XPath;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Providers.EquipmentSorters;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Utils;
using SafeFluentXPath.Implementation.Api;

namespace DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Repositories
{
    public class EquipmentRosterEquipmentPoolRepository : IEquipmentPoolRepository
    {
        internal const string EquipmentRostersRootTag = "EquipmentRosters";
        private const string EquipmentRosterNodeName = "EquipmentRoster";
        private const string EquipmentSetNodeName = "EquipmentSet";

        private readonly IXmlProcessor _xmlProcessor;

        public EquipmentRosterEquipmentPoolRepository(IXmlProcessor xmlProcessor)
        {
            _xmlProcessor = xmlProcessor;
        }

        public IDictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>> GetEquipmentPoolsById()
        {
            return _xmlProcessor
                .GetXmlNodes(EquipmentRostersRootTag).XPathSelectElements(EquipmentRosterXPath())
                .Aggregate(new Dictionary<string, IEquipmentSorter>(),
                    (equipmentPools, equipmentNode) =>
                    {
                        var equipmentRosterId = (string)equipmentNode.XPathEvaluate("string(../@id)");

                        if (!equipmentPools.ContainsKey(equipmentRosterId))
                            equipmentPools[equipmentRosterId] = new EquipmentPoolSorter();

                        ((EquipmentPoolSorter)equipmentPools[equipmentRosterId]).AddEquipmentLoadout(
                            equipmentNode);
                        return equipmentPools;
                    })
                .ToDictionary(characterEquipmentPools => characterEquipmentPools.Key,
                    characterEquipmentPools => characterEquipmentPools.Value.GetEquipmentPools());
        }

        private string EquipmentRosterXPath()
        {
            return new XPathBuilder()
                .Element(EquipmentRostersRootTag)
                .ChildElement(EquipmentRosterNodeName)
                .ChildElement(EquipmentSetNodeName)
                .Build();
        }
    }
}