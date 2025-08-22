using System.Collections.Generic;
using DellarteDellaGuerra.Cannon.UI.Model;

namespace DellarteDellaGuerra.Cannon.UI;

public class SiegeEngineIconRepository : ISiegeEngineIconRepository
{
    private static readonly ISet<SiegeEngineIcon> _siegeEngineIcons = new HashSet<SiegeEngineIcon>
    {
        new("Falconet", "falconet")
    };

    public ISet<SiegeEngineIcon> SiegeEngineIcons => _siegeEngineIcons;
}