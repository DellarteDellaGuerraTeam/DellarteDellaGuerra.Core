using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Infrastructure.SiegeEngines.Port;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace DellarteDellaGuerra.Infrastructure.SiegeEngines;

public class CannonAvailabilityProvider : ICannonAvailabilityProvider
{
    private readonly ICannonRegistry _cannonRegistry;

    public CannonAvailabilityProvider(ICannonRegistry cannonRegistry)
    {
        _cannonRegistry = cannonRegistry;
    }

    public IEnumerable<SiegeEngineType> GetAvailableCannonTypes(PartyBase party, BattleSideEnum side) =>
        _cannonRegistry.GetAllCannonTypes()
            .Select(ct => ct.GetSiegeEngineType())
            .Where(se => se != null);
}
