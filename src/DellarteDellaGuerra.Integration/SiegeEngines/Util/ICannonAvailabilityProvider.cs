using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace DellarteDellaGuerra.Integration.SiegeEngines.Util
{
    public interface ICannonAvailabilityProvider
    {
        IEnumerable<SiegeEngineType> GetAvailableCannons(PartyBase party, BattleSideEnum side);
    }
}