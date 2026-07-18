using System;
using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.PrivateWars;
using DellarteDellaGuerra.Domain.PrivateWars.Model;

namespace DellarteDellaGuerra.PrivateWars.Api.Armies
{
    public record PrivateWarArmyCandidate(
        string PartyId,
        string ClanId,
        bool IsEligible,
        float SiegeStrength);

    public record ArmyFormationDecision(
        bool CanCreateArmy,
        IReadOnlyList<string> MemberPartyIds);

    public class PrivateWarArmyDecisionAdapter
    {
        private readonly PrivateWarArmyPolicy _policy;

        public PrivateWarArmyDecisionAdapter(PrivateWarArmyPolicy policy)
        {
            _policy = policy;
        }

        public PrivateWarArmyPlan? CreatePlan(
            PrivateWar war,
            WarSide side,
            string leaderPartyId,
            IReadOnlyList<PrivateWarArmyCandidate> candidates,
            IReadOnlyList<PrivateWarArmyAssignment> assignments,
            Func<string, string?> getSuzerain,
            bool kingdomHasSettlements,
            int maximumMemberCount)
        {
            var plan = _policy.CreatePlan(
                war,
                side,
                leaderPartyId,
                candidates.Select(candidate => new PrivateWarArmyParty(
                    candidate.PartyId, candidate.ClanId, candidate.IsEligible)).ToList(),
                assignments,
                getSuzerain);
            if (plan is null) return null;

            var limitedMemberIds = plan.MemberPartyIds.Take(maximumMemberCount).ToList();
            var leader = candidates.First(candidate => candidate.PartyId == plan.LeaderPartyId);
            var members = candidates
                .Where(candidate => limitedMemberIds.Contains(candidate.PartyId))
                .ToList();
            if (members.Count == 0
                || kingdomHasSettlements
                && leader.SiegeStrength + members.Sum(member => member.SiegeStrength) < 1000f)
            {
                return null;
            }

            return plan with { MemberPartyIds = limitedMemberIds };
        }

        public ArmyFormationDecision FilterOrdinaryMembers(
            bool vanillaCanCreateArmy,
            string leaderClanId,
            float leaderSiegeStrength,
            bool kingdomHasSettlements,
            IReadOnlyList<PrivateWarArmyCandidate> candidates,
            Func<string, string, bool> areEnemies)
        {
            if (!vanillaCanCreateArmy)
                return new ArmyFormationDecision(false, Array.Empty<string>());

            var members = candidates
                .Where(candidate => !areEnemies(leaderClanId, candidate.ClanId))
                .ToList();
            if (members.Count == 0
                || kingdomHasSettlements
                && leaderSiegeStrength + members.Sum(member => member.SiegeStrength) < 1000f)
            {
                return new ArmyFormationDecision(false, Array.Empty<string>());
            }

            return new ArmyFormationDecision(
                true, members.Select(member => member.PartyId).ToList());
        }

        public IReadOnlyList<string>? SelectPrivateMembers(
            IReadOnlyList<float> earlierScores,
            float privateWarScore,
            IReadOnlyList<string> privateWarMemberPartyIds)
        {
            return earlierScores.Any(score => score >= privateWarScore)
                ? null
                : privateWarMemberPartyIds;
        }
    }
}
