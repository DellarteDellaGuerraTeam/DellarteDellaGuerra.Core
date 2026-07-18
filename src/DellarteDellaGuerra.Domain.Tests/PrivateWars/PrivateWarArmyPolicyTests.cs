using System;
using System.Collections.Generic;
using DellarteDellaGuerra.Domain.PrivateWars;
using DellarteDellaGuerra.Domain.PrivateWars.Model;

namespace DellarteDellaGuerra.Domain.Tests.PrivateWars
{
    public class PrivateWarArmyPolicyTests
    {
        private readonly PrivateWarArmyPolicy _policy = new();

        [Fact]
        public void CreatePlan_EligiblePrincipalPartyHasExclusiveLeadershipPriority()
        {
            var war = PrivateWarTestData.War(attacker: "A", defender: "D");
            var parties = new List<PrivateWarArmyParty>
            {
                new("principal_party", "A", IsEligible: true),
                new("participant_party", "A_vassal", IsEligible: true)
            };
            var suzerain = Suzerain(("A_vassal", "A"));

            var principalPlan = _policy.CreatePlan(
                war, WarSide.Attacker, "principal_party", parties,
                Array.Empty<PrivateWarArmyAssignment>(), suzerain);
            var participantPlan = _policy.CreatePlan(
                war, WarSide.Attacker, "participant_party", parties,
                Array.Empty<PrivateWarArmyAssignment>(), suzerain);

            Assert.Equal("principal_party", principalPlan?.LeaderPartyId);
            Assert.Null(participantPlan);
        }

        [Fact]
        public void CreatePlan_ParticipatingClanMayLeadWhenNoPrincipalPartyIsEligible()
        {
            var war = PrivateWarTestData.War(attacker: "A", defender: "D");
            var parties = new List<PrivateWarArmyParty>
            {
                new("principal_party", "A", IsEligible: false),
                new("participant_party", "A_vassal", IsEligible: true)
            };

            var plan = _policy.CreatePlan(
                war, WarSide.Attacker, "participant_party", parties,
                Array.Empty<PrivateWarArmyAssignment>(), Suzerain(("A_vassal", "A")));

            Assert.Equal("participant_party", plan?.LeaderPartyId);
        }

        [Fact]
        public void CreatePlan_RejectsSecondLeaderForSameWarSideAndGoal()
        {
            var war = PrivateWarTestData.War(attacker: "A", defender: "D");
            var parties = new List<PrivateWarArmyParty>
            {
                new("existing_leader", "A", IsEligible: true),
                new("candidate_leader", "A", IsEligible: true)
            };
            var assignments = new[]
            {
                new PrivateWarArmyAssignment(
                    war.Id, WarSide.Attacker, war.MainGoalSettlementId, "existing_leader")
            };

            var plan = _policy.CreatePlan(
                war, WarSide.Attacker, "candidate_leader", parties, assignments, Suzerain());

            Assert.Null(plan);
        }

        [Fact]
        public void CreatePlan_DoesNotReplaceExistingFallbackLeaderWhenPrincipalBecomesEligible()
        {
            var war = PrivateWarTestData.War(attacker: "A", defender: "D");
            var parties = new List<PrivateWarArmyParty>
            {
                new("fallback_leader", "A_vassal", IsEligible: true),
                new("principal_party", "A", IsEligible: true)
            };
            var assignments = new[]
            {
                new PrivateWarArmyAssignment(
                    war.Id, WarSide.Attacker, war.MainGoalSettlementId, "fallback_leader")
            };

            var fallbackPlan = _policy.CreatePlan(
                war, WarSide.Attacker, "fallback_leader", parties, assignments,
                Suzerain(("A_vassal", "A")));
            var principalPlan = _policy.CreatePlan(
                war, WarSide.Attacker, "principal_party", parties, assignments,
                Suzerain(("A_vassal", "A")));

            Assert.Equal("fallback_leader", fallbackPlan?.LeaderPartyId);
            Assert.Null(principalPlan);
        }

        [Fact]
        public void CreatePlan_IncludesParticipatingPartiesFromTheSameSide()
        {
            var war = PrivateWarTestData.War(attacker: "A", defender: "D");
            var parties = new List<PrivateWarArmyParty>
            {
                new("leader", "A", IsEligible: true),
                new("same_side_member", "A_vassal", IsEligible: true)
            };

            var plan = _policy.CreatePlan(
                war, WarSide.Attacker, "leader", parties,
                Array.Empty<PrivateWarArmyAssignment>(), Suzerain(("A_vassal", "A")));

            Assert.Equal(new[] { "same_side_member" }, plan?.MemberPartyIds);
        }

        [Fact]
        public void CreatePlan_ExcludesOpposingAndUninvolvedPartiesFromMembership()
        {
            var war = PrivateWarTestData.War(attacker: "A", defender: "D");
            var parties = new List<PrivateWarArmyParty>
            {
                new("leader", "A", IsEligible: true),
                new("same_side_member", "A_vassal", IsEligible: true),
                new("opposing_party", "D_vassal", IsEligible: true),
                new("uninvolved_party", "X", IsEligible: true)
            };
            var suzerain = Suzerain(("A_vassal", "A"), ("D_vassal", "D"));

            var plan = _policy.CreatePlan(
                war, WarSide.Attacker, "leader", parties,
                Array.Empty<PrivateWarArmyAssignment>(), suzerain);

            Assert.Equal(new[] { "same_side_member" }, plan?.MemberPartyIds);
        }

        [Fact]
        public void CreatePlan_RejectsIneligibleLeader()
        {
            var war = PrivateWarTestData.War(attacker: "A", defender: "D");
            var parties = new[]
            {
                new PrivateWarArmyParty("leader", "A", IsEligible: false)
            };

            var plan = _policy.CreatePlan(
                war, WarSide.Attacker, "leader", parties,
                Array.Empty<PrivateWarArmyAssignment>(), Suzerain());

            Assert.Null(plan);
        }

        [Fact]
        public void CreatePlan_ExcludesIneligiblePartyFromMembership()
        {
            var war = PrivateWarTestData.War(attacker: "A", defender: "D");
            var parties = new[]
            {
                new PrivateWarArmyParty("leader", "A", IsEligible: true),
                new PrivateWarArmyParty("ineligible_member", "A_vassal", IsEligible: false)
            };

            var plan = _policy.CreatePlan(
                war, WarSide.Attacker, "leader", parties,
                Array.Empty<PrivateWarArmyAssignment>(), Suzerain(("A_vassal", "A")));

            Assert.Empty(plan?.MemberPartyIds ?? Array.Empty<string>());
        }

        [Fact]
        public void CreatePlan_AssociatesPlanAndExistingAssignmentWithFrozenGoal()
        {
            var war = PrivateWarTestData.War(
                attacker: "A", defender: "D", mainGoal: "frozen_goal");
            var parties = new[]
            {
                new PrivateWarArmyParty("leader", "A", IsEligible: true)
            };
            var assignments = new[]
            {
                new PrivateWarArmyAssignment(
                    war.Id, WarSide.Attacker, "other_goal", "other_leader")
            };

            var plan = _policy.CreatePlan(
                war, WarSide.Attacker, "leader", parties, assignments, Suzerain());

            Assert.Equal("frozen_goal", plan?.GoalSettlementId);
        }

        private static Func<string, string?> Suzerain(params (string clan, string suzerain)[] links)
        {
            var map = new Dictionary<string, string>();
            foreach (var (clan, suzerain) in links) map[clan] = suzerain;
            return clan => map.TryGetValue(clan, out var s) ? s : null;
        }
    }
}
