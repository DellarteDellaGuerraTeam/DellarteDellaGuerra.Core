using System.Collections.Generic;
using Bannerlord.PrivateWars.Domain.Declaration;

namespace DellarteDellaGuerra.Domain.Tests.PrivateWars
{
    public class MainGoalSelectorTests
    {
        private readonly MainGoalSelector _selector = new();

        [Fact]
        public void Select_PicksHighestProsperityDeJureSettlementHeldByDefender()
        {
            var settlements = new List<SettlementInfo>
            {
                new("town_low", "D", Prosperity: 1000f),
                new("town_high", "D", Prosperity: 5000f),
                new("castle_other", "X", Prosperity: 9000f) // held by someone else
            };

            Assert.Equal("town_high", _selector.Select(settlements, defenderClanId: "D"));
        }

        [Fact]
        public void Select_DefenderHoldsNoneOfTheTitle_ReturnsNull()
        {
            var settlements = new List<SettlementInfo>
            {
                new("town_x", "X", Prosperity: 5000f)
            };

            Assert.Null(_selector.Select(settlements, defenderClanId: "D"));
        }
    }
}
