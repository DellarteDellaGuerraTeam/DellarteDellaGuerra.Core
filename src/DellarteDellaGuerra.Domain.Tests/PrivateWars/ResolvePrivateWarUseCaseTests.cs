using System;
using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.PrivateWars;
using DellarteDellaGuerra.Domain.PrivateWars.Model;

namespace DellarteDellaGuerra.Domain.Tests.PrivateWars
{
    public class ResolvePrivateWarUseCaseTests
    {
        private readonly ResolvePrivateWarUseCase _useCase = new();

        // Two principals A (attacker) and D (defender); everyone else uninvolved.
        private static Func<string, WarSide?> TwoSides() =>
            clan => clan == "A" ? WarSide.Attacker : clan == "D" ? WarSide.Defender : (WarSide?)null;

        [Fact]
        public void Execute_WhitePeace_RevertsEveryMovedFiefAndLosesClaim()
        {
            var snapshot = new Dictionary<string, string>
            {
                ["settlement_goal"] = "D", // defender's main goal, now taken by A
                ["town_a"] = "A"           // attacker's town, now taken by D
            };
            var war = PrivateWarTestData.War(attacker: "A", defender: "D", mainGoal: "settlement_goal", fiefSnapshot: snapshot);
            var currentOwners = new Dictionary<string, string> { ["settlement_goal"] = "A", ["town_a"] = "D" };

            var plan = _useCase.Execute(war, PrivateWarOutcome.WhitePeace, currentOwners, TwoSides());

            Assert.Null(plan.Prize);
            Assert.True(plan.AttackerClaimLost);
            Assert.Equal(
                new[] { ("settlement_goal", "D"), ("town_a", "A") }.OrderBy(t => t.Item1),
                plan.Reverts.Select(r => (r.SettlementId, r.RevertToClanId)).OrderBy(t => t.Item1));
        }

        [Fact]
        public void Execute_AttackerVictory_GoalBecomesPrizeAndRestReverts()
        {
            var snapshot = new Dictionary<string, string>
            {
                ["settlement_goal"] = "D",
                ["town_a"] = "A"
            };
            var war = PrivateWarTestData.War(attacker: "A", defender: "D", title: "title_county",
                mainGoal: "settlement_goal", fiefSnapshot: snapshot);
            var currentOwners = new Dictionary<string, string> { ["settlement_goal"] = "A", ["town_a"] = "D" };

            var plan = _useCase.Execute(war, PrivateWarOutcome.AttackerVictory, currentOwners, TwoSides());

            Assert.False(plan.AttackerClaimLost);
            Assert.NotNull(plan.Prize);
            Assert.Equal("settlement_goal", plan.Prize!.SettlementId);
            Assert.Equal("title_county", plan.Prize.TitleId);
            Assert.Equal("A", plan.Prize.NewHolderClanId);

            // Goal is NOT in the reverts; the recaptured attacker town is.
            var revert = Assert.Single(plan.Reverts);
            Assert.Equal("town_a", revert.SettlementId);
            Assert.Equal("A", revert.RevertToClanId);
        }

        [Fact]
        public void Execute_FiefHeldByThirdParty_IsSkipped()
        {
            // Shared-goal case (§18.E): a snapshotted defender fief is now held by B, a third party
            // to this A-vs-D war. This war must not move it.
            var snapshot = new Dictionary<string, string> { ["town_contested"] = "D" };
            var war = PrivateWarTestData.War(attacker: "A", defender: "D", fiefSnapshot: snapshot);
            var currentOwners = new Dictionary<string, string> { ["town_contested"] = "B" };

            var plan = _useCase.Execute(war, PrivateWarOutcome.WhitePeace, currentOwners, TwoSides());

            Assert.Empty(plan.Reverts);
        }

        [Fact]
        public void Execute_UnmovedFief_ProducesNoRevert()
        {
            var snapshot = new Dictionary<string, string> { ["town_a"] = "A" };
            var war = PrivateWarTestData.War(attacker: "A", defender: "D", fiefSnapshot: snapshot);
            var currentOwners = new Dictionary<string, string> { ["town_a"] = "A" }; // unchanged

            var plan = _useCase.Execute(war, PrivateWarOutcome.WhitePeace, currentOwners, TwoSides());

            Assert.Empty(plan.Reverts);
        }
    }
}
