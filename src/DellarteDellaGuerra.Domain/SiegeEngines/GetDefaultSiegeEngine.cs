using DellarteDellaGuerra.Domain.SiegeEngines.Model;

namespace DellarteDellaGuerra.Domain.SiegeEngines
{
    public class GetDefaultSiegeEngine
    {
        public SiegeEngine GetSiegeEngine()
        {
            return new SiegeEngine("falconet");
        }
    }
}