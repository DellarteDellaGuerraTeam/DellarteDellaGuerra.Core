using System.Collections.Generic;
using DellarteDellaGuerra.Domain.SiegeEngines.Model;

namespace DellarteDellaGuerra.Domain.SiegeEngines.Port
{
    public interface ICannonConfigurationReader
    {
        IEnumerable<CannonProperties> LoadCannonProperties();
    }
}