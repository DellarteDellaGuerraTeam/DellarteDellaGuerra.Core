using System.Collections.Generic;

namespace DellarteDellaGuerra.Domain.Church.Hierarchy
{
    public class ChurchMap
    {
        public IReadOnlyList<ChurchMapEntry> Roots { get; }

        public ChurchMap(IReadOnlyList<ChurchMapEntry> roots)
        {
            Roots = roots;
        }
    }
}
