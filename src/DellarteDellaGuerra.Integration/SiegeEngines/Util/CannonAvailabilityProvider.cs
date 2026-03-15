using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Infrastructure.SiegeEngines.Port;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace DellarteDellaGuerra.Integration.SiegeEngines.Util;

public class CannonAvailabilityProvider : ICannonAvailabilityProvider
{
    private readonly ICannonRegistry _cannonRegistry;
    private readonly MBObjectManager _mbObjectManager;

    public CannonAvailabilityProvider(ICannonRegistry cannonRegistry, MBObjectManager mbObjectManager)
    {
        _cannonRegistry = cannonRegistry;
        _mbObjectManager = mbObjectManager;
    }

    public IEnumerable<SiegeEngineType> GetAvailableCannons(PartyBase party, BattleSideEnum side) =>
        _cannonRegistry.GetAllCannons()
            .Select(cannon => _mbObjectManager.GetObject<SiegeEngineType>(cannon.Id))
            .Where(se => se != null)
            .ToList();
}
