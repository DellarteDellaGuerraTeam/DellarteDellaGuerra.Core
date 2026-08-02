using System;
using System.Collections.Generic;
using Bannerlord.PrivateWars.Domain;
using Bannerlord.PrivateWars.Domain.Armies;

namespace DellarteDellaGuerra.Domain.Tests.PrivateWars
{
    public class PrivateWarArmyPolicyTests
    {
        [Fact]
        public void CreatePlan_EligiblePrincipalPartyHasExclusiveLeadershipPriority()
        {
            var war = PrivateWarTestData.War(attacker: "A", defender: "D");
            var parties = new List<ArmyParty>
            {
                Party("principal_party", "A"),
                Party("participant_party", "A_vassal")
            };
            var policy = Policy(("A_vassal", "A"));

            var principalPlan = policy.CreatePlan(
                war, WarSide.Attacker, "principal_party", parties,
                Array.Empty<ArmyAssignment>(), maximumMemberCount: 2);
            var participantPlan = policy.CreatePlan(
                war, WarSide.Attacker, "participant_party", parties,
                Array.Empty<ArmyAssignment>(), maximumMemberCount: 2);

            Assert.Equal("principal_party", principalPlan?.LeaderPartyId);
            Assert.Null(participantPlan);
        }

        [Fact]
        public void CreatePlan_ParticipatingClanMayLeadWhenNoPrincipalPartyIsEligible()
        {
            var war = PrivateWarTestData.War(attacker: "A", defender: "D");
            var parties = new List<ArmyParty>
            {
                Party("principal_party", "A", canLead: false, canJoin: false),
                Party("participant_party", "A_vassal")
            };

            var plan = Policy(("A_vassal", "A")).CreatePlan(
                war, WarSide.Attacker, "participant_party", parties,
                Array.Empty<ArmyAssignment>(),
                maximumMemberCount: 2);

            Assert.Equal("participant_party", plan?.LeaderPartyId);
        }

        [Fact]
        public void CreatePlan_RejectsSecondLeaderForSameWarSideAndGoal()
        {
            var war = PrivateWarTestData.War(attacker: "A", defender: "D");
            var parties = new List<ArmyParty>
            {
                Party("existing_leader", "A"),
                Party("candidate_leader", "A")
            };
            var assignments = new[]
            {
                new ArmyAssignment(
                    war.Id, WarSide.Attacker, war.MainGoalSettlementId, "existing_leader")
            };

            var plan = Policy().CreatePlan(
                war, WarSide.Attacker, "candidate_leader", parties, assignments,
                maximumMemberCount: 2);

            Assert.Null(plan);
        }

        [Fact]
        public void CreatePlan_DoesNotReplaceExistingFallbackLeaderWhenPrincipalBecomesEligible()
        {
            var war = PrivateWarTestData.War(attacker: "A", defender: "D");
            var parties = new List<ArmyParty>
            {
                Party("fallback_leader", "A_vassal"),
                Party("principal_party", "A")
            };
            var assignments = new[]
            {
                new ArmyAssignment(
                    war.Id, WarSide.Attacker, war.MainGoalSettlementId, "fallback_leader")
            };
            var policy = Policy(("A_vassal", "A"));

            var fallbackPlan = policy.CreatePlan(
                war, WarSide.Attacker, "fallback_leader", parties, assignments,
                maximumMemberCount: 2);
            var principalPlan = policy.CreatePlan(
                war, WarSide.Attacker, "principal_party", parties, assignments,
                maximumMemberCount: 2);

            Assert.Equal("fallback_leader", fallbackPlan?.LeaderPartyId);
            Assert.Null(principalPlan);
        }

        [Fact]
        public void CreatePlan_IncludesParticipatingPartiesFromTheSameSide()
        {
            var war = PrivateWarTestData.War(attacker: "A", defender: "D");
            var parties = new List<ArmyParty>
            {
                Party("leader", "A"),
                Party("same_side_member", "A_vassal")
            };

            var plan = Policy(("A_vassal", "A")).CreatePlan(
                war, WarSide.Attacker, "leader", parties,
                Array.Empty<ArmyAssignment>(),
                maximumMemberCount: 2);

            Assert.Equal(new[] { "same_side_member" }, plan?.MemberPartyIds);
        }

        [Fact]
        public void CreatePlan_ExcludesOpposingAndUninvolvedPartiesFromMembership()
        {
            var war = PrivateWarTestData.War(attacker: "A", defender: "D");
            var parties = new List<ArmyParty>
            {
                Party("leader", "A"),
                Party("same_side_member", "A_vassal"),
                Party("opposing_party", "D_vassal"),
                Party("uninvolved_party", "X")
            };

            var plan = Policy(("A_vassal", "A"), ("D_vassal", "D")).CreatePlan(
                war, WarSide.Attacker, "leader", parties,
                Array.Empty<ArmyAssignment>(), maximumMemberCount: 4);

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

            var plan = Policy().CreatePlan(
                war, WarSide.Attacker, "leader", parties,
                Array.Empty<ArmyAssignment>(), maximumMemberCount: 1);

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

            var plan = Policy(("A_vassal", "A")).CreatePlan(
                war, WarSide.Attacker, "leader", parties,
                Array.Empty<ArmyAssignment>(),
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
                new ArmyAssignment(
                    war.Id, WarSide.Attacker, "other_goal", "other_leader")
            };

            var plan = Policy().CreatePlan(
                war, WarSide.Attacker, "leader", parties, assignments,
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

            var plan = Policy(("A_vassal", "A")).CreatePlan(
                war, WarSide.Attacker, "fallback", parties,
                Array.Empty<ArmyAssignment>(),
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

            var plan = Policy(("A_vassal", "A"), ("A_subvassal", "A_vassal")).CreatePlan(
                war, WarSide.Attacker, "leader", parties,
                Array.Empty<ArmyAssignment>(),
                maximumMemberCount: 2);

            Assert.Equal(new[] { "highest", "middle" }, plan?.MemberPartyIds);
        }

        private static ArmyParty Party(
            string partyId,
            string clanId,
            bool canLead = true,
            bool canJoin = true,
            float desirability = 0f)
            => new(partyId, clanId, canLead, canJoin, desirability);

        private static ArmyPolicy Policy(params (string clan, string suzerain)[] suzerains)
            => new(new WarSideResolver(new MapSuzerainProvider(suzerains)));
    }
}
