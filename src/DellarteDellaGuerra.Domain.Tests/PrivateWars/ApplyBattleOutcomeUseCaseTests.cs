using Bannerlord.PrivateWars.Domain;
using Bannerlord.PrivateWars.Domain.Model;

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
        public void Measure_SmallWinAgainstLargeSide_ScoresSmallBump()
        {
            // Regression: a 200-strength party routed while its side fields 1800 elsewhere is 10% of
            // the side, not the full weight (old normalization used only the battle's losing parties).
            var outcome = BattleOutcome.Measure(WarSide.Attacker, defeatedInBattle: 200f, new[]
            {
                (WarSide.Defender, 1000f),
                (WarSide.Defender, 800f),
            });

            var result = _useCase.Execute(PrivateWarTestData.War(battleScore: 0f), outcome);

            Assert.Equal(2000f, outcome.LosingSideTotalStrength);
            Assert.Equal(5f, result.BattleScore);
        }

        [Fact]
        public void Measure_IgnoresWinningSideParties()
        {
            var outcome = BattleOutcome.Measure(WarSide.Defender, defeatedInBattle: 300f, new[]
            {
                (WarSide.Defender, 5000f),
                (WarSide.Attacker, 300f),
            });

            Assert.Equal(600f, outcome.LosingSideTotalStrength);
        }

        [Fact]
        public void Measure_LosingSideWipedOut_ScoresFullWeight()
        {
            var outcome = BattleOutcome.Measure(
                WarSide.Attacker, defeatedInBattle: 700f, new (WarSide, float)[0]);

            var result = _useCase.Execute(PrivateWarTestData.War(battleScore: 0f), outcome);

            Assert.Equal(ApplyBattleOutcomeUseCase.BattleWeight, result.BattleScore);
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
