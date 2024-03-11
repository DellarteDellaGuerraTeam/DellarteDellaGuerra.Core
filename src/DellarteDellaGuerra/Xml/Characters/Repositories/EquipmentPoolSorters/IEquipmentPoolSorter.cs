using System.Collections.Generic;
using System.Xml.Linq;

namespace DellarteDellaGuerra.Xml.Characters.Repositories.EquipmentPoolSorters
{
    public interface IEquipmentPoolSorter
    {
        IReadOnlyCollection<IReadOnlyCollection<XNode>> GetEquipmentPools();
    }
}
