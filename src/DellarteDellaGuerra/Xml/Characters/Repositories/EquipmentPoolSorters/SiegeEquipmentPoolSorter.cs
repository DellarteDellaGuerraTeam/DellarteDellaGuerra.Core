using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;

namespace DellarteDellaGuerra.Xml.Characters.Repositories.EquipmentPoolSorters
{
    public class SiegeEquipmentPoolSorter : IEquipmentPoolSorter
    {
        private readonly IEquipmentPoolSorter _equipmentPoolSorter;

        public SiegeEquipmentPoolSorter(IEquipmentPoolSorter equipmentPoolSorter)
        {
            _equipmentPoolSorter = equipmentPoolSorter;
        }

        private bool MatchesCriteria(XNode node)
        {
            // If the siege attribute is true, then it is a siege equipment.
            bool.TryParse((string) node.XPathEvaluate("string(@siege)"), out var isSiege);
            return isSiege;
        }

        public IReadOnlyCollection<IReadOnlyCollection<XNode>> GetEquipmentPools()
        {
            return _equipmentPoolSorter
                .GetEquipmentPools()
                .Select(equipment => equipment.Where(MatchesCriteria).ToList())
                .Where(equipment => equipment.Any())
                .ToList();
        }
    }
}
