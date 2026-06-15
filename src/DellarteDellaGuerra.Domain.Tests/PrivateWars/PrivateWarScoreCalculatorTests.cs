using DellarteDellaGuerra.Domain.PrivateWars;
using DellarteDellaGuerra.Domain.PrivateWars.Model;

namespace DellarteDellaGuerra.Domain.Tests.PrivateWars
{
    public class PrivateWarScoreCalculatorTests
    {
        private readonly PrivateWarScoreCalculator _calculator = new();

        [Fact]
        public void Compute_AttackerHoldsMainGoal_FatigueDrivesToPlus100()
        {
            var obs = PrivateWarTestData.NoControl with { AttackerHoldsMainGoal = true };

            // 50 (goal) + 1/day * 1000 days, clamped to the +100 victory limit.
            Assert.Equal(100f, _calculator.Compute(obs, daysElapsed: 1000f));
        }

        [Fact]
        public void Compute_DefenderHoldsMainGoal_FatigueDrivesToMinus100()
        {
            var obs = PrivateWarTestData.NoControl; // AttackerHoldsMainGoal = false

            Assert.Equal(-100f, _calculator.Compute(obs, daysElapsed: 1000f));
        }

        [Fact]
        public void Compute_SettlementWeights_TownCastleAndGoal()
        {
            var obs = PrivateWarTestData.NoControl with
            {
                AttackerHoldsMainGoal = true,
                DefenderSideTownsHeldByAttacker = 1,
                DefenderSideCastlesHeldByAttacker = 1
            };

            // 50 + 30 + 10, no time elapsed (no fatigue).
            Assert.Equal(90f, _calculator.Compute(obs, daysElapsed: 0f));
        }

        [Fact]
        public void Compute_DefenderHoldingAttackerFiefs_SubtractsFromScore()
        {
            var obs = PrivateWarTestData.NoControl with
            {
                AttackerHoldsMainGoal = true,
                AttackerSideTownsHeldByDefender = 1,
                AttackerSideCastlesHeldByDefender = 1
            };

            // 50 - 30 - 10.
            Assert.Equal(10f, _calculator.Compute(obs, daysElapsed: 0f));
        }

        [Fact]
        public void Compute_PrisonerTerm_FivePerOpposingPrincipalClanCaptive()
        {
            var attackerHolds = PrivateWarTestData.NoControl with
            {
                DefenderClanPrisonersHeldByAttackerSide = 3
            };
            Assert.Equal(15f, _calculator.Compute(attackerHolds, daysElapsed: 0f));

            var defenderHolds = PrivateWarTestData.NoControl with
            {
                AttackerClanPrisonersHeldByDefenderSide = 2
            };
            Assert.Equal(-10f, _calculator.Compute(defenderHolds, daysElapsed: 0f));
        }

        [Fact]
        public void Compute_AccumulatedBattleScore_AddedToTotal()
        {
            var obs = PrivateWarTestData.NoControl with
            {
                AttackerHoldsMainGoal = true,
                AccumulatedBattleScore = 20f
            };

            Assert.Equal(70f, _calculator.Compute(obs, daysElapsed: 0f));
        }

        [Fact]
        public void Compute_ClampsToScoreLimit()
        {
            var obs = PrivateWarTestData.NoControl with
            {
                AttackerHoldsMainGoal = true,
                DefenderSideTownsHeldByAttacker = 10 // 50 + 300 -> clamp +100
            };

            Assert.Equal(100f, _calculator.Compute(obs, daysElapsed: 0f));
        }
    }
}
