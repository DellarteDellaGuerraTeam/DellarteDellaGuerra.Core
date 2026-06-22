using DellarteDellaGuerra.Domain.PrivateWars;
using DellarteDellaGuerra.Domain.PrivateWars.Model;

namespace DellarteDellaGuerra.Domain.Tests.PrivateWars
{
    public class TickPrivateWarUseCaseTests
    {
        private readonly TickPrivateWarUseCase _useCase = new(new PrivateWarScoreCalculator());

        [Fact]
        public void Execute_AttackerReachesLimit_ReportsAttackerVictory()
        {
            var war = PrivateWarTestData.War(startDay: 0f);
            var obs = PrivateWarTestData.NoControl with { AttackerHoldsMainGoal = true };

            var result = _useCase.Execute(war, obs, currentDay: 1000f);

            Assert.Equal(100f, result.Score);
            Assert.Equal(PrivateWarOutcome.AttackerVictory, result.Outcome);
        }

        [Fact]
        public void Execute_DefenderReachesLimit_ReportsDefenderVictory()
        {
            var war = PrivateWarTestData.War(startDay: 0f);
            var obs = PrivateWarTestData.NoControl; // attacker does not hold the goal

            var result = _useCase.Execute(war, obs, currentDay: 1000f);

            Assert.Equal(-100f, result.Score);
            Assert.Equal(PrivateWarOutcome.DefenderVictory, result.Outcome);
        }

        [Fact]
        public void Execute_BelowThreshold_NoOutcome()
        {
            var war = PrivateWarTestData.War(startDay: 0f);
            var obs = PrivateWarTestData.NoControl with { AttackerHoldsMainGoal = true };

            var result = _useCase.Execute(war, obs, currentDay: 5f); // 50 + 5 = 55

            Assert.Equal(55f, result.Score);
            Assert.Null(result.Outcome);
        }

        [Fact]
        public void Execute_DaysElapsedMeasuredFromGoalLastTakenDay()
        {
            // Fatigue drifts from the day the goal was last taken, not the war's start day, so a
            // retaken goal restarts the climb from zero.
            var war = PrivateWarTestData.War(startDay: 0f, goalLastTakenDay: 100f);
            var obs = PrivateWarTestData.NoControl with { AttackerHoldsMainGoal = true };

            var result = _useCase.Execute(war, obs, currentDay: 110f); // 50 + 10

            Assert.Equal(60f, result.Score);
        }
    }
}
