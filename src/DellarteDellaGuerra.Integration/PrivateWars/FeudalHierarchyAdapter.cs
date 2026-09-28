using Bannerlord.PrivateWars.Domain.Port;
using DellarteDellaGuerra.Domain.Titles;

namespace DellarteDellaGuerra.Integration.PrivateWars
{
    // Adapts the existing feudal suzerain query to the ISuzerainProvider port the private-war
    // registry walks to resolve war sides, keeping the two domain features decoupled.
    public class FeudalHierarchyAdapter : ISuzerainProvider
    {
        private readonly SuzeraintyPolicy _suzeraintyPolicy;

        public FeudalHierarchyAdapter(SuzeraintyPolicy suzeraintyPolicy)
        {
            _suzeraintyPolicy = suzeraintyPolicy;
        }

        public string? GetSuzerain(string clanId) => _suzeraintyPolicy.GetSuzerain(clanId);
    }
}