using DellarteDellaGuerra.Domain.PrivateWars;
using DellarteDellaGuerra.Domain.PrivateWars.Model;
using DellarteDellaGuerra.PrivateWars.Api.Armies;

namespace DellarteDellaGuerra.Tests.PrivateWars;

public class PrivateWarArmyDecisionAdapterTests
{
    [Fact]
    public void CreatePlan_GivesEligiblePrincipalPartyPriority()
    {
        var adapter = new PrivateWarArmyDecisionAdapter(new PrivateWarArmyPolicy());
        var candidates = new[]
        {
            new PrivateWarArmyCandidate("principal", "A", true, 600f),
            new PrivateWarArmyCandidate("vassal", "A_vassal", true, 500f)
        };
        var war = War();
        Func<string, string?> suzerain = clan => clan == "A_vassal" ? "A" : null;

        var principal = adapter.CreatePlan(
            war, WarSide.Attacker, "principal", candidates,
            Array.Empty<PrivateWarArmyAssignment>(), suzerain,
            kingdomHasSettlements: true, maximumMemberCount: 2);
        var vassal = adapter.CreatePlan(
            war, WarSide.Attacker, "vassal", candidates,
            Array.Empty<PrivateWarArmyAssignment>(), suzerain,
            kingdomHasSettlements: true, maximumMemberCount: 2);

        Assert.Equal("principal", principal?.LeaderPartyId);
        Assert.Null(vassal);
    }

    [Fact]
    public void CreatePlan_AllowsParticipatingClanFallbackWhenPrincipalIsIneligible()
    {
        var adapter = new PrivateWarArmyDecisionAdapter(new PrivateWarArmyPolicy());
        var candidates = new[]
        {
            new PrivateWarArmyCandidate("principal", "A", false, 600f),
            new PrivateWarArmyCandidate("fallback", "A_vassal", true, 600f),
            new PrivateWarArmyCandidate("member", "A_subvassal", true, 500f)
        };
        Func<string, string?> suzerain = clan => clan switch
        {
            "A_vassal" => "A",
            "A_subvassal" => "A_vassal",
            _ => null
        };

        var plan = adapter.CreatePlan(
            War(), WarSide.Attacker, "fallback", candidates,
            Array.Empty<PrivateWarArmyAssignment>(), suzerain,
            kingdomHasSettlements: true, maximumMemberCount: 2);

        Assert.Equal("fallback", plan?.LeaderPartyId);
        Assert.Equal(new[] { "member" }, plan?.MemberPartyIds);
    }

    [Fact]
    public void FilterOrdinaryMembers_ExcludesPrivateEnemyAndFailsWeakenedFormation()
    {
        var adapter = new PrivateWarArmyDecisionAdapter(new PrivateWarArmyPolicy());
        var candidates = new[]
        {
            new PrivateWarArmyCandidate("ally", "A_vassal", true, 400f),
            new PrivateWarArmyCandidate("private_enemy", "D", true, 600f)
        };

        var decision = adapter.FilterOrdinaryMembers(
            vanillaCanCreateArmy: true,
            leaderClanId: "A",
            leaderSiegeStrength: 500f,
            kingdomHasSettlements: true,
            candidates,
            (left, right) => left == "A" && right == "D");

        Assert.False(decision.CanCreateArmy);
        Assert.Empty(decision.MemberPartyIds);
    }

    [Fact]
    public void FilterOrdinaryMembers_RetainsAuthorizedMembersWhenFormationRemainsStrongEnough()
    {
        var adapter = new PrivateWarArmyDecisionAdapter(new PrivateWarArmyPolicy());
        var candidates = new[]
        {
            new PrivateWarArmyCandidate("ally", "A_vassal", true, 600f),
            new PrivateWarArmyCandidate("private_enemy", "D", true, 600f)
        };

        var decision = adapter.FilterOrdinaryMembers(
            vanillaCanCreateArmy: true,
            leaderClanId: "A",
            leaderSiegeStrength: 500f,
            kingdomHasSettlements: true,
            candidates,
            (left, right) => left == "A" && right == "D");

        Assert.True(decision.CanCreateArmy);
        Assert.Equal(new[] { "ally" }, decision.MemberPartyIds);
    }

    [Fact]
    public void CreatePlan_RejectsPrivateArmyThatFailsVanillaStrengthContract()
    {
        var adapter = new PrivateWarArmyDecisionAdapter(new PrivateWarArmyPolicy());
        var candidates = new[]
        {
            new PrivateWarArmyCandidate("leader", "A", true, 500f),
            new PrivateWarArmyCandidate("same_side", "A_vassal", true, 400f)
        };

        var plan = adapter.CreatePlan(
            War(), WarSide.Attacker, "leader", candidates,
            Array.Empty<PrivateWarArmyAssignment>(),
            clan => clan == "A_vassal" ? "A" : null,
            kingdomHasSettlements: true, maximumMemberCount: 1);

        Assert.Null(plan);
    }

    [Fact]
    public void CreatePlan_FormsPrivateOnlyArmyFromAuthorizedSameSideMembers()
    {
        var adapter = new PrivateWarArmyDecisionAdapter(new PrivateWarArmyPolicy());
        var candidates = new[]
        {
            new PrivateWarArmyCandidate("leader", "A", true, 600f),
            new PrivateWarArmyCandidate("same_side", "A_vassal", true, 500f),
            new PrivateWarArmyCandidate("enemy", "D", true, 900f),
            new PrivateWarArmyCandidate("uninvolved", "X", true, 900f)
        };

        var plan = adapter.CreatePlan(
            War(), WarSide.Attacker, "leader", candidates,
            Array.Empty<PrivateWarArmyAssignment>(),
            clan => clan == "A_vassal" ? "A" : null,
            kingdomHasSettlements: true, maximumMemberCount: 3);

        Assert.Equal(new[] { "same_side" }, plan?.MemberPartyIds);
    }

    [Fact]
    public void CreatePlan_RestrictsMembersToVanillaCapacityInCandidateOrder()
    {
        var adapter = new PrivateWarArmyDecisionAdapter(new PrivateWarArmyPolicy());
        var candidates = new[]
        {
            new PrivateWarArmyCandidate("leader", "A", true, 600f),
            new PrivateWarArmyCandidate("first", "A_vassal", true, 500f),
            new PrivateWarArmyCandidate("second", "A_subvassal", true, 900f)
        };

        var plan = adapter.CreatePlan(
            War(), WarSide.Attacker, "leader", candidates,
            Array.Empty<PrivateWarArmyAssignment>(),
            clan => clan switch
            {
                "A_vassal" => "A",
                "A_subvassal" => "A_vassal",
                _ => null
            },
            kingdomHasSettlements: true,
            maximumMemberCount: 1);

        Assert.Equal(new[] { "first" }, plan?.MemberPartyIds);
    }

    [Theory]
    [InlineData(false, true, "leader", "goal", "leader", "goal")]
    [InlineData(true, false, "leader", "goal", "leader", "goal")]
    [InlineData(true, true, "leader", "goal", "other", "goal")]
    [InlineData(true, true, "leader", "goal", "leader", "other_goal")]
    public void DecidePrivateCreation_LeavesUnrelatedCreationUnchanged(
        bool isBesieger,
        bool hasActivePrivateWar,
        string leaderPartyId,
        string targetSettlementId,
        string privateWarLeaderPartyId,
        string frozenGoalSettlementId)
    {
        var adapter = new PrivateWarArmyDecisionAdapter(new PrivateWarArmyPolicy());
        var revalidationCalls = 0;

        var decision = adapter.DecidePrivateCreation(
            isBesieger,
            hasActivePrivateWar,
            leaderPartyId,
            targetSettlementId,
            privateWarLeaderPartyId,
            frozenGoalSettlementId,
            () =>
            {
                revalidationCalls++;
                return new[] { "private_member" };
            });

        Assert.Equal(PrivateWarArmyCreationAction.ContinueOriginal, decision.Action);
        Assert.Equal(0, revalidationCalls);
    }

    [Fact]
    public void DecidePrivateCreation_RevalidatesMatchingCreationAndReplacesMembers()
    {
        var adapter = new PrivateWarArmyDecisionAdapter(new PrivateWarArmyPolicy());
        var authorizedMembers = new[] { "fresh_member" };
        var revalidationCalls = 0;

        var decision = adapter.DecidePrivateCreation(
            isBesieger: true,
            hasActivePrivateWar: true,
            leaderPartyId: "leader",
            targetSettlementId: "goal",
            privateWarLeaderPartyId: "leader",
            frozenGoalSettlementId: "goal",
            revalidateMembers: () =>
            {
                revalidationCalls++;
                return authorizedMembers;
            });

        Assert.Equal(PrivateWarArmyCreationAction.ReplaceMembers, decision.Action);
        Assert.Same(authorizedMembers, decision.MemberPartyIds);
        Assert.Equal(1, revalidationCalls);
    }

    [Fact]
    public void DecidePrivateCreation_SuppressesMatchingCreationWhenRevalidationFails()
    {
        var adapter = new PrivateWarArmyDecisionAdapter(new PrivateWarArmyPolicy());

        var decision = adapter.DecidePrivateCreation(
            isBesieger: true,
            hasActivePrivateWar: true,
            leaderPartyId: "leader",
            targetSettlementId: "goal",
            privateWarLeaderPartyId: "leader",
            frozenGoalSettlementId: "goal",
            revalidateMembers: () => null);

        Assert.Equal(PrivateWarArmyCreationAction.Suppress, decision.Action);
        Assert.Empty(decision.MemberPartyIds);
    }

    private static PrivateWar War()
        => new(
            "war", "A", "D", "claim", "title", "goal",
            new Dictionary<string, string>(), 0f, 0f, 0f, 0f,
            PrivateWarStatus.Active);
}
