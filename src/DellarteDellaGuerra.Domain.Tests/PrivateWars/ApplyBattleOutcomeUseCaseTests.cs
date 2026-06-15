using DellarteDellaGuerra.Domain.PrivateWars;
using DellarteDellaGuerra.Domain.PrivateWars.Model;

namespace DellarteDellaGuerra.Domain.Tests.PrivateWars
{
    public class ApplyBattleOutcomeUseCaseTests
    {
        private readonly ApplyBattleOutcomeUseCase _useCase = new();

        [Fact]
        public void Execute_AttackerWin_AddsNormalizedPositiveBump()
        {
            var war = PrivateWarTestData.War(battleScore: 0f);

            // Wiped half of a total 1000-strength side -> 0.5 * 50 = +25.
            var result = _useCase.Execute(
                war, new BattleOutcome(WarSide.Attacker, EnemyForceDefeated: 500f, LosingSideTotalStrength: 1000f));

            Assert.Equal(25f, result.BattleScore);
        }

        [Fact]
        public void Execute_DefenderWin_SubtractsBump()
        {
            var war = PrivateWarTestData.War(battleScore: 0f);

            var result = _useCase.Execute(
                war, new BattleOutcome(WarSide.Defender, EnemyForceDefeated: 500f, LosingSideTotalStrength: 1000f));

            Assert.Equal(-25f, result.BattleScore);
        }

        [Fact]
        public void Execute_Accumulates_AcrossBattles()
        {
            var war = PrivateWarTestData.War(battleScore: 10f);

            var result = _useCase.Execute(
                war, new BattleOutcome(WarSide.Attacker, EnemyForceDefeated: 200f, LosingSideTotalStrength: 1000f));

            Assert.Equal(20f, result.BattleScore); // 10 + 0.2*50
        }

        [Fact]
        public void Execute_ClampsToBattleScoreCap()
        {
            var war = PrivateWarTestData.War(battleScore: 40f);

            var result = _useCase.Execute(
                war, new BattleOutcome(WarSide.Attacker, EnemyForceDefeated: 1000f, LosingSideTotalStrength: 1000f));

            Assert.Equal(ApplyBattleOutcomeUseCase.BattleScoreCap, result.BattleScore);
        }

        [Fact]
        public void Execute_NonPositiveLosingStrength_IsNoOp()
        {
            var war = PrivateWarTestData.War(battleScore: 12f);

            var result = _useCase.Execute(
                war, new BattleOutcome(WarSide.Attacker, EnemyForceDefeated: 100f, LosingSideTotalStrength: 0f));

            Assert.Equal(12f, result.BattleScore);
        }
    }
}
