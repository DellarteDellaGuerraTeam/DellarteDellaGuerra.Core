using System.Linq;
using DellarteDellaGuerra.Domain.Tournament.Reward.Port;
using TaleWorlds.CampaignSystem.Settlements;

namespace DellarteDellaGuerra.Tournament.Reward.Spi
{
    public class HighestTownProsperityProvider : IHighestTownProsperityProvider
    {
        public float GetHighestTownProsperity()
        {
            return Settlement.All.Where(s => s.IsTown).Max(t => t.Town.Prosperity);
        }
    }
}