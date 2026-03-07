using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace DellarteDellaGuerra.Infrastructure.SiegeEngines.Port;

public interface ICannonAvailabilityProvider
{
    IEnumerable<SiegeEngineType> GetAvailableCannons(PartyBase party, BattleSideEnum side);
}
