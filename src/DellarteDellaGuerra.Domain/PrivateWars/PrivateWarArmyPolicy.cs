using System;
using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.PrivateWars.Model;

namespace DellarteDellaGuerra.Domain.PrivateWars
{
    public class PrivateWarArmyPolicy
    {
        public PrivateWarArmyPlan? CreatePlan(
            PrivateWar war,
            WarSide side,
            string candidateLeaderPartyId,
            IReadOnlyList<PrivateWarArmyParty> parties,
            IReadOnlyList<PrivateWarArmyAssignment> assignments,
            Func<string, string?> getSuzerain)
        {
            var candidate = parties.FirstOrDefault(p => p.PartyId == candidateLeaderPartyId);
            if (candidate is null || !candidate.IsEligible) return null;

            var currentAssignment = assignments.FirstOrDefault(a =>
                a.PrivateWarId == war.Id
                && a.Side == side
                && a.GoalSettlementId == war.MainGoalSettlementId);
            if (currentAssignment is not null
                && currentAssignment.LeaderPartyId != candidateLeaderPartyId)
            {
                return null;
            }
            var isCurrentLeader = currentAssignment?.LeaderPartyId == candidateLeaderPartyId;

            var principalClanId = side == WarSide.Attacker
                ? war.AttackerPrincipalClanId
                : war.DefenderPrincipalClanId;
            var sideResolver = new WarSideResolver();
            var hasEligiblePrincipalParty = parties.Any(p =>
                p.IsEligible && p.ClanId == principalClanId);

            if (!isCurrentLeader
                && hasEligiblePrincipalParty
                && candidate.ClanId != principalClanId)
            {
                return null;
            }
            if (sideResolver.ResolveSide(candidate.ClanId, war, getSuzerain) != side) return null;

            var memberPartyIds = parties
                .Where(p => p.PartyId != candidate.PartyId)
                .Where(p => p.IsEligible)
                .Where(p => sideResolver.ResolveSide(p.ClanId, war, getSuzerain) == side)
                .Select(p => p.PartyId)
                .ToList();

            return new PrivateWarArmyPlan(
                war.MainGoalSettlementId,
                candidate.PartyId,
                memberPartyIds);
        }
    }
}
