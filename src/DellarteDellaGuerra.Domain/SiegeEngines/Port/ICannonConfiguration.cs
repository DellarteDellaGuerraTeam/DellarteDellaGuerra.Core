using System.Collections.Generic;
using DellarteDellaGuerra.Domain.SiegeEngines.Model;

namespace DellarteDellaGuerra.Domain.SiegeEngines.Port
{
    public interface ICannonConfiguration
    {
        IEnumerable<CannonProperties> LoadCannonProperties();
    }
}