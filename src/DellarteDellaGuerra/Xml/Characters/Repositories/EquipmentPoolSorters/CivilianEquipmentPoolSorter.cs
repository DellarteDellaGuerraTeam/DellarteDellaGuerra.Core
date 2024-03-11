using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;

namespace DellarteDellaGuerra.Xml.Characters.Repositories.EquipmentPoolSorters
{
    public class CivilianEquipmentPoolSorter : IEquipmentPoolSorter
    {
        private readonly IEquipmentPoolSorter _equipmentPoolSorter;

        public CivilianEquipmentPoolSorter(IEquipmentPoolSorter equipmentPoolSorter)
        {
            _equipmentPoolSorter = equipmentPoolSorter;
        }

        private bool MatchesCriteria(XNode node)
        {
            // If the civilian attribute is true, then it is a civilian equipment
            bool.TryParse((string) node.XPathEvaluate("string(@civilian)"), out var isSiege);
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