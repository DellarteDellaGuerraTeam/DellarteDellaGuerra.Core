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
                Party("principal_party", "A"),
                Party("participant_party", "A_vassal")
            };
            var suzerain = Suzerain(("A_vassal", "A"));

            var principalPlan = _policy.CreatePlan(
                war, WarSide.Attacker, "principal_party", parties,
                Array.Empty<PrivateWarArmyAssignment>(), suzerain, maximumMemberCount: 2);
            var participantPlan = _policy.CreatePlan(
                war, WarSide.Attacker, "participant_party", parties,
                Array.Empty<PrivateWarArmyAssignment>(), suzerain, maximumMemberCount: 2);

            Assert.Equal("principal_party", principalPlan?.LeaderPartyId);
            Assert.Null(participantPlan);
        }

        [Fact]
        public void CreatePlan_ParticipatingClanMayLeadWhenNoPrincipalPartyIsEligible()
        {
            var war = PrivateWarTestData.War(attacker: "A", defender: "D");
            var parties = new List<PrivateWarArmyParty>
            {
                Party("principal_party", "A", canLead: false, canJoin: false),
                Party("participant_party", "A_vassal")
            };

            var plan = _policy.CreatePlan(
                war, WarSide.Attacker, "participant_party", parties,
                Array.Empty<PrivateWarArmyAssignment>(), Suzerain(("A_vassal", "A")),
                maximumMemberCount: 2);

            Assert.Equal("participant_party", plan?.LeaderPartyId);
        }

        [Fact]
        public void CreatePlan_RejectsSecondLeaderForSameWarSideAndGoal()
        {
            var war = PrivateWarTestData.War(attacker: "A", defender: "D");
            var parties = new List<PrivateWarArmyParty>
            {
                Party("existing_leader", "A"),
                Party("candidate_leader", "A")
            };
            var assignments = new[]
            {
                new PrivateWarArmyAssignment(
                    war.Id, WarSide.Attacker, war.MainGoalSettlementId, "existing_leader")
            };

            var plan = _policy.CreatePlan(
                war, WarSide.Attacker, "candidate_leader", parties, assignments, Suzerain(),
                maximumMemberCount: 2);

            Assert.Null(plan);
        }

        [Fact]
        public void CreatePlan_DoesNotReplaceExistingFallbackLeaderWhenPrincipalBecomesEligible()
        {
            var war = PrivateWarTestData.War(attacker: "A", defender: "D");
            var parties = new List<PrivateWarArmyParty>
            {
                Party("fallback_leader", "A_vassal"),
                Party("principal_party", "A")
            };
            var assignments = new[]
            {
                new PrivateWarArmyAssignment(
                    war.Id, WarSide.Attacker, war.MainGoalSettlementId, "fallback_leader")
            };

            var fallbackPlan = _policy.CreatePlan(
                war, WarSide.Attacker, "fallback_leader", parties, assignments,
                Suzerain(("A_vassal", "A")), maximumMemberCount: 2);
            var principalPlan = _policy.CreatePlan(
                war, WarSide.Attacker, "principal_party", parties, assignments,
                Suzerain(("A_vassal", "A")), maximumMemberCount: 2);

            Assert.Equal("fallback_leader", fallbackPlan?.LeaderPartyId);
            Assert.Null(principalPlan);
        }

        [Fact]
        public void CreatePlan_IncludesParticipatingPartiesFromTheSameSide()
        {
            var war = PrivateWarTestData.War(attacker: "A", defender: "D");
            var parties = new List<PrivateWarArmyParty>
            {
                Party("leader", "A"),
                Party("same_side_member", "A_vassal")
            };

            var plan = _policy.CreatePlan(
                war, WarSide.Attacker, "leader", parties,
                Array.Empty<PrivateWarArmyAssignment>(), Suzerain(("A_vassal", "A")),
                maximumMemberCount: 2);

            Assert.Equal(new[] { "same_side_member" }, plan?.MemberPartyIds);
        }

        [Fact]
        public void CreatePlan_ExcludesOpposingAndUninvolvedPartiesFromMembership()
        {
            var war = PrivateWarTestData.War(attacker: "A", defender: "D");
            var parties = new List<PrivateWarArmyParty>
            {
                Party("leader", "A"),
                Party("same_side_member", "A_vassal"),
                Party("opposing_party", "D_vassal"),
                Party("uninvolved_party", "X")
            };
            var suzerain = Suzerain(("A_vassal", "A"), ("D_vassal", "D"));

            var plan = _policy.CreatePlan(
                war, WarSide.Attacker, "leader", parties,
                Array.Empty<PrivateWarArmyAssignment>(), suzerain, maximumMemberCount: 4);

            Assert.Equal(new[] { "same_side_member" }, plan?.MemberPartyIds);
        }

        [Fact]
        public void CreatePlan_RejectsIneligibleLeader()
        {
            var war = PrivateWarTestData.War(attacker: "A", defender: "D");
            var parties = new[]
            {
                Party("leader", "A", canLead: false, canJoin: true)
            };

            var plan = _policy.CreatePlan(
                war, WarSide.Attacker, "leader", parties,
                Array.Empty<PrivateWarArmyAssignment>(), Suzerain(), maximumMemberCount: 1);

            Assert.Null(plan);
        }

        [Fact]
        public void CreatePlan_ExcludesIneligiblePartyFromMembership()
        {
            var war = PrivateWarTestData.War(attacker: "A", defender: "D");
            var parties = new[]
            {
                Party("leader", "A"),
                Party("ineligible_member", "A_vassal", canLead: false, canJoin: false)
            };

            var plan = _policy.CreatePlan(
                war, WarSide.Attacker, "leader", parties,
                Array.Empty<PrivateWarArmyAssignment>(), Suzerain(("A_vassal", "A")),
                maximumMemberCount: 2);

            Assert.Empty(plan?.MemberPartyIds ?? Array.Empty<string>());
        }

        [Fact]
        public void CreatePlan_AssociatesPlanAndExistingAssignmentWithFrozenGoal()
        {
            var war = PrivateWarTestData.War(
                attacker: "A", defender: "D", mainGoal: "frozen_goal");
            var parties = new[]
            {
                Party("leader", "A")
            };
            var assignments = new[]
            {
                new PrivateWarArmyAssignment(
                    war.Id, WarSide.Attacker, "other_goal", "other_leader")
            };

            var plan = _policy.CreatePlan(
                war, WarSide.Attacker, "leader", parties, assignments, Suzerain(),
                maximumMemberCount: 1);

            Assert.Equal("frozen_goal", plan?.GoalSettlementId);
        }

        [Fact]
        public void CreatePlan_PrincipalJoinableButUnableToLead_DoesNotBlockFallbackLeader()
        {
            var war = PrivateWarTestData.War(attacker: "A", defender: "D");
            var parties = new[]
            {
                Party("principal_member", "A", canLead: false, canJoin: true, desirability: 5f),
                Party("fallback", "A_vassal", canLead: true, canJoin: true, desirability: 1f)
            };

            var plan = _policy.CreatePlan(
                war, WarSide.Attacker, "fallback", parties,
                Array.Empty<PrivateWarArmyAssignment>(), Suzerain(("A_vassal", "A")),
                maximumMemberCount: 1);

            Assert.Equal("fallback", plan?.LeaderPartyId);
            Assert.Equal(new[] { "principal_member" }, plan?.MemberPartyIds);
        }

        [Fact]
        public void CreatePlan_OrdersJoinableMembersByDescendingDesirabilityBeforeApplyingCapacity()
        {
            var war = PrivateWarTestData.War(attacker: "A", defender: "D");
            var parties = new[]
            {
                Party("leader", "A"),
                Party("lower", "A_vassal", desirability: 2f),
                Party("highest", "A_subvassal", desirability: 8f),
                Party("middle", "A_vassal", desirability: 5f)
            };

            var plan = _policy.CreatePlan(
                war, WarSide.Attacker, "leader", parties,
                Array.Empty<PrivateWarArmyAssignment>(),
                Suzerain(("A_vassal", "A"), ("A_subvassal", "A_vassal")),
                maximumMemberCount: 2);

            Assert.Equal(new[] { "highest", "middle" }, plan?.MemberPartyIds);
        }

        private static PrivateWarArmyParty Party(
            string partyId,
            string clanId,
            bool canLead = true,
            bool canJoin = true,
            float desirability = 0f)
            => new(partyId, clanId, canLead, canJoin, desirability);

        private static Func<string, string?> Suzerain(params (string clan, string suzerain)[] links)
        {
            var map = new Dictionary<string, string>();
            foreach (var (clan, suzerain) in links) map[clan] = suzerain;
            return clan => map.TryGetValue(clan, out var s) ? s : null;
        }
    }
}
