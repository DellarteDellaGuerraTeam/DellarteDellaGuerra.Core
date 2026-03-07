using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Infrastructure.SiegeEngines.Port;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace DellarteDellaGuerra.Infrastructure.SiegeEngines;

public class CannonAvailabilityProvider : ICannonAvailabilityProvider
{
    private readonly ICannonRegistry _cannonRegistry;
    private readonly MBObjectManager _mbObjectManager;

    public CannonAvailabilityProvider(ICannonRegistry cannonRegistry, MBObjectManager mbObjectManager)
    {
        _cannonRegistry = cannonRegistry;
        _mbObjectManager = mbObjectManager;
    }

    public IEnumerable<SiegeEngineType> GetAvailableCannonTypes(PartyBase party, BattleSideEnum side) =>
        _cannonRegistry.GetAllCannonTypes()
            .Select(cannon => _mbObjectManager.GetObject<SiegeEngineType>(cannon.Id))
            .Where(se => se != null);
}
