using DellarteDellaGuerra.Domain.SiegeEngines.Model;

namespace DellarteDellaGuerra.Domain.SiegeEngines
{
    public class GetDefaultSiegeEngine
    {
        public SiegeEngine GetAttackerSiegeEngine()
        {
            return new SiegeEngine("falconet");
        }
        
        public SiegeEngine GetDefenderSiegeEngine()
        {
            return new SiegeEngine("veuglaire");
        }
    }
}