using System.Collections.Generic;
using Bannerlord.PrivateWars.Domain;
using Bannerlord.PrivateWars.Domain.Scoring;

namespace DellarteDellaGuerra.Domain.Tests.PrivateWars
{
    public class PrivateWarObservationBuilderTests
    {
        [Fact]
        public void Build_CountsFiefsThatCrossedSidesTracksGoalAndPassesTallies()
        {
            var war = PrivateWarTestData.War(
                attacker: "A", defender: "D", mainGoal: "goal", battleScore: 7f);
            var fiefs = new List<FiefSnapshot>
            {
                Fief("goal", current: "A", original: "D", town: true),          // goal held by attacker
                Fief("d_town", current: "A", original: "D", town: true),        // D -> A town
                Fief("d_castle", current: "A", original: "D", castle: true),    // D -> A castle
                Fief("a_town", current: "D", original: "A", town: true),        // A -> D town
                Fief("a_castle", current: "D", original: "A", castle: true),    // A -> D castle
                Fief("still_a", current: "A", original: "A", town: true)        // no crossing
            };

            var observations = ObservationBuilder.Build(
                war, fiefs, Resolver(), defenderPrisoners: 3, attackerPrisoners: 2);

            Assert.True(observations.AttackerHoldsMainGoal);
            Assert.Equal(1, observations.DefenderSideTownsHeldByAttacker);
            Assert.Equal(1, observations.DefenderSideCastlesHeldByAttacker);
            Assert.Equal(1, observations.AttackerSideTownsHeldByDefender);
            Assert.Equal(1, observations.AttackerSideCastlesHeldByDefender);
            Assert.Equal(3, observations.DefenderClanPrisonersHeldByAttackerSide);
            Assert.Equal(2, observations.AttackerClanPrisonersHeldByDefenderSide);
            Assert.Equal(7f, observations.AccumulatedBattleScore);
        }

        [Fact]
        public void Build_ResolvesOwnersThroughTheSuzerainChain()
        {
            var war = PrivateWarTestData.War(attacker: "A", defender: "D", mainGoal: "goal");
            // A vassal now holds a town whose original owner was a D vassal: a defender-side town
            // captured by the attacker side, resolved entirely through the suzerain chain.
            var fiefs = new List<FiefSnapshot>
            {
                Fief("town", current: "A_vassal", original: "D_vassal", town: true)
            };

            var observations = ObservationBuilder.Build(
                war, fiefs, Resolver(), defenderPrisoners: 0, attackerPrisoners: 0);

            Assert.Equal(1, observations.DefenderSideTownsHeldByAttacker);
        }

        [Fact]
        public void Build_IgnoresUninvolvedOwnersAndReportsGoalNotHeldByAttacker()
        {
            var war = PrivateWarTestData.War(attacker: "A", defender: "D", mainGoal: "goal");
            var fiefs = new List<FiefSnapshot>
            {
                Fief("goal", current: "D", original: "D", town: true),      // goal held by defender
                Fief("neutral", current: "X", original: "D", town: true)    // uninvolved current owner
            };

            var observations = ObservationBuilder.Build(
                war, fiefs, Resolver(), defenderPrisoners: 0, attackerPrisoners: 0);

            Assert.False(observations.AttackerHoldsMainGoal);
            Assert.Equal(0, observations.DefenderSideTownsHeldByAttacker);
            Assert.Equal(0, observations.AttackerSideTownsHeldByDefender);
        }

        private static FiefSnapshot Fief(
            string settlementId,
            string current,
            string original,
            bool town = false,
            bool castle = false)
            => new(settlementId, current, original, town, castle);

        private static WarSideResolver Resolver()
            => new(new MapSuzerainProvider(("A_vassal", "A"), ("D_vassal", "D")));
    }
}
