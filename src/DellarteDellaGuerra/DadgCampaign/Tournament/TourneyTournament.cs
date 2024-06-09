using System.Linq;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Core;

namespace DellarteDellaGuerra.DadgCampaign.Tournament
{
    public class TourneyTournament : FightTournamentGame
    {
        public TourneyTournament(Town town) : base(town)
        {
        }

        protected override ItemObject GetTournamentPrize(
            bool includePlayer,
            int lastRecordedLordCountForTournamentPrize)
        {
            return Items.All.First(item =>
                !item.HasFoodComponent && !item.IsTradeGood && !item.IsFood && !item.IsAnimal &&
                item.Tier == ItemObject.ItemTiers.Tier3);
        }
    }
}