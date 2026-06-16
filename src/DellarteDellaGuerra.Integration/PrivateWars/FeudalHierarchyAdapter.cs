using DellarteDellaGuerra.Domain.PrivateWars.Port;
using DellarteDellaGuerra.Domain.Titles;

namespace DellarteDellaGuerra.Integration.PrivateWars
{
    // Adapts the existing feudal suzerain query to the IFeudalHierarchy port the private-war
    // registry walks to resolve war sides, keeping the two domain features decoupled.
    public class FeudalHierarchyAdapter : IFeudalHierarchy
    {
        private readonly IGetSuzerainUseCase _getSuzerain;

        public FeudalHierarchyAdapter(IGetSuzerainUseCase getSuzerain)
        {
            _getSuzerain = getSuzerain;
        }

        public string? GetSuzerain(string clanId) => _getSuzerain.Execute(clanId);
    }
}