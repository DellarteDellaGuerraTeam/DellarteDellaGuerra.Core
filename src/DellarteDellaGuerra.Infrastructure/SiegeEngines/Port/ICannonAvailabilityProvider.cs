using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace DellarteDellaGuerra.Infrastructure.SiegeEngines.Port;

public interface ICannonAvailabilityProvider
{
    IEnumerable<SiegeEngineType> GetAvailableCannonTypes(PartyBase party, BattleSideEnum side);
}
