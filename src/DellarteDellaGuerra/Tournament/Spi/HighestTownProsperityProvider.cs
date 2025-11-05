using System.Linq;
using DellarteDellaGuerra.Domain.Tournament.Port;
using TaleWorlds.CampaignSystem.Settlements;

namespace DellarteDellaGuerra.Tournament.Spi
{
    public class HighestTownProsperityProvider : IHighestTownProsperityProvider
    {
        public float GetHighestTownProsperity()
        {
            return Settlement.All.Where(s => s.IsTown).Max(t => t.Town.Prosperity);
        }
    }
}