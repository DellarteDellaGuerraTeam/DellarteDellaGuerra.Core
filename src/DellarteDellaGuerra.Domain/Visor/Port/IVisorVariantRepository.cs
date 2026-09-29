using System.Collections.Generic;

namespace DellarteDellaGuerra.Domain.Visor.Port
{
    public interface IVisorVariantRepository
    {
        IReadOnlyList<VisorVariantPair> LoadPairs();
    }
}
