using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.SiegeEngines;
using DellarteDellaGuerra.Infrastructure.SiegeEngines.Port;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace DellarteDellaGuerra.Infrastructure.SiegeEngines;

public class CannonAvailabilityProvider
{
    private readonly CannonRegistry _cannonRegistry;

    public CannonAvailabilityProvider(CannonRegistry cannonRegistry)
    {
        _cannonRegistry = cannonRegistry;
    }

    public IEnumerable<SiegeEngineType> GetAvailableCannonTypes(PartyBase party, BattleSideEnum side)
    {
        return _cannonRegistry.GetAllCannonTypes()
            .Select(ct =>
            {
                if (ct is ICannonType bannerlordCannonType) return bannerlordCannonType.GetSiegeEngineType();
                return null;
            })
            .Where(se => se != null);
    }
}