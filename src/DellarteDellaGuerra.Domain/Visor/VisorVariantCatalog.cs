using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.Visor.Port;

namespace DellarteDellaGuerra.Domain.Visor
{
    /// <summary>
    /// Holds the known closed/open helmet item pairs.
    /// </summary>
    public class VisorVariantCatalog
    {
        private readonly IReadOnlyList<VisorVariantPair> _pairs;

        public VisorVariantCatalog(IVisorVariantRepository repository)
        {
            _pairs = repository.LoadPairs();
        }

        public bool TryFindPair(string itemId, out VisorVariantPair pair)
        {
            // netstandard2.0 has no [MaybeNullWhen]; callers must only read pair when this returns true.
            pair = _pairs.FirstOrDefault(p => p.ClosedItemId == itemId || p.OpenItemId == itemId)!;
            return pair != null;
        }
    }
}
