using System.Collections.Generic;

namespace DellarteDellaGuerra.Domain.Tests.PrivateWars
{
    public class PrivateWarFiefTests
    {
        [Fact]
        public void IsWarFief_CoversGoalAndSnapshottedFiefsOnly()
        {
            var war = PrivateWarTestData.War(mainGoal: "goal",
                fiefSnapshot: new Dictionary<string, string> { ["castle_a"] = "clan_attacker" });

            Assert.True(war.IsWarFief("goal"));
            Assert.True(war.IsWarFief("castle_a"));
            Assert.False(war.IsWarFief("other"));
        }
    }
}
