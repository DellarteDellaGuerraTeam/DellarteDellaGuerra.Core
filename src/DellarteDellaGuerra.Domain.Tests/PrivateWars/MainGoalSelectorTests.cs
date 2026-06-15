using System.Collections.Generic;
using DellarteDellaGuerra.Domain.PrivateWars;
using DellarteDellaGuerra.Domain.PrivateWars.Model;

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
                new("town_low", "D", IsTown: true, Prosperity: 1000f),
                new("town_high", "D", IsTown: true, Prosperity: 5000f),
                new("castle_other", "X", IsTown: false, Prosperity: 9000f) // held by someone else
            };

            Assert.Equal("town_high", _selector.Select(settlements, defenderClanId: "D", defenderCapitalId: "capital"));
        }

        [Fact]
        public void Select_DefenderHoldsNoneOfTheTitle_FallsBackToCapital()
        {
            var settlements = new List<SettlementInfo>
            {
                new("town_x", "X", IsTown: true, Prosperity: 5000f)
            };

            Assert.Equal("capital", _selector.Select(settlements, defenderClanId: "D", defenderCapitalId: "capital"));
        }

        [Fact]
        public void Select_LandlessDefender_ReturnsNull()
        {
            var settlements = new List<SettlementInfo>
            {
                new("town_x", "X", IsTown: true, Prosperity: 5000f)
            };

            Assert.Null(_selector.Select(settlements, defenderClanId: "D", defenderCapitalId: null));
        }
    }
}
