using System.Linq;
using DellarteDellaGuerra.Domain.Tournament.Port;
using TaleWorlds.CampaignSystem.Settlements;
using Town = DellarteDellaGuerra.Domain.Tournament.Model.Town;

namespace DellarteDellaGuerra.Tournament.Spi
{
    public class TownRepository : ITownRepository
    {
        public Town? GetTown(string id)
        {
            Settlement? settlement = Settlement.All.FirstOrDefault(s => s.StringId.Equals(id));
            if (settlement?.Town?.Prosperity is null) return null;
            return new Town(id, settlement.Town.Prosperity);
        }
    }
}