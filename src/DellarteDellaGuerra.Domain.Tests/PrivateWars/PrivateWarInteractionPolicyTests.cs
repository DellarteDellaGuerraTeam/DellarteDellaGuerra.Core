using DellarteDellaGuerra.Domain.PrivateWars;

namespace DellarteDellaGuerra.Domain.Tests.PrivateWars;

public class PrivateWarInteractionPolicyTests
{
    private readonly PrivateWarInteractionPolicy _policy = new();

    [Theory]
    [InlineData(false, false, true, false, ReinforcementSide.Enemy)]
    [InlineData(false, false, false, true, ReinforcementSide.Player)]
    [InlineData(false, false, false, false, ReinforcementSide.None)]
    [InlineData(true, false, false, false, ReinforcementSide.Enemy)]
    public void ResolveReinforcementSide_CombinesPrivateAndVanillaHostility(
        bool vanillaEnemyOfPlayer,
        bool vanillaEnemyOfEncounteredParty,
        bool privateEnemyOfPlayer,
        bool privateEnemyOfEncounteredParty,
        ReinforcementSide expected)
    {
        var result = _policy.ResolveReinforcementSide(
            vanillaEnemyOfPlayer,
            vanillaEnemyOfEncounteredParty,
            privateEnemyOfPlayer,
            privateEnemyOfEncounteredParty);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(true, 4, -1)]
    [InlineData(false, 4, 4)]
    [InlineData(false, 2, 2)]
    [InlineData(false, -1, -1)]
    public void RestrictRecruitableIndex_BlocksOnlyPrivateEnemies(
        bool arePrivateEnemies,
        int vanillaMaximumIndex,
        int expected)
    {
        var result = _policy.RestrictRecruitableIndex(arePrivateEnemies, vanillaMaximumIndex);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("private enemy", true, false)]
    [InlineData("same-side participant", false, true)]
    [InlineData("uninvolved same-kingdom clan", false, true)]
    [InlineData("normal cross-kingdom enemy", false, true)]
    public void ShouldRunRecruitmentEntry_BlocksOnlyPrivateEnemies(
        string _,
        bool arePrivateEnemies,
        bool expected)
    {
        Assert.Equal(expected, _policy.ShouldRunRecruitmentEntry(arePrivateEnemies));
    }

    [Theory]
    [InlineData("private enemy", true, true, false)]
    [InlineData("same-side participant", true, false, true)]
    [InlineData("uninvolved same-kingdom clan", true, false, true)]
    [InlineData("normal cross-kingdom enemy", false, false, false)]
    public void AllowSettlementVisit_PreservesVanillaAndExcludesPrivateEnemies(
        string _,
        bool vanillaSuitable,
        bool arePrivateEnemies,
        bool expected)
    {
        Assert.Equal(expected, _policy.AllowSettlementVisit(vanillaSuitable, arePrivateEnemies));
    }

    [Theory]
    [InlineData(false, 6f, 12f, 6f)]
    [InlineData(true, 6f, 12f, 15f)]
    public void ResolveEncounterJoiningRadius_MatchesVanillaPlayerSiegeBranch(
        bool hasActivePlayerSiege,
        float normalEncounterRadius,
        float settlementDefendingWaitingPositionRadius,
        float expected)
    {
        var result = _policy.ResolveEncounterJoiningRadius(
            hasActivePlayerSiege,
            normalEncounterRadius,
            settlementDefendingWaitingPositionRadius);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("ordinary faction war", true, false, true)]
    [InlineData("active private war", false, true, true)]
    [InlineData("ransom and time escape remain eligible", false, false, false)]
    public void ResolveCaptivityWarPredicate_ChangesOnlyThePrivateWarNoMoreEnemiesDecision(
        string _,
        bool vanillaAtWar,
        bool areCaptiveAndCaptorPrivateEnemies,
        bool expected)
    {
        Assert.Equal(
            expected,
            _policy.ResolveCaptivityWarPredicate(vanillaAtWar, areCaptiveAndCaptorPrivateEnemies));
    }

    [Theory]
    [InlineData("ordinary faction enemy", true, false, true)]
    [InlineData("active private-war besieger", false, true, true)]
    [InlineData("same-side or uninvolved party", false, false, false)]
    public void ResolveSallyOutStrengthEnemy_CombinesVanillaAndPrivateHostility(
        string _,
        bool vanillaEnemies,
        bool partyAndOwnerPrivateEnemies,
        bool expected)
    {
        Assert.Equal(
            expected,
            _policy.ResolveSallyOutStrengthEnemy(vanillaEnemies, partyAndOwnerPrivateEnemies));
    }

    [Theory]
    [InlineData(true, true, false, true)]
    [InlineData(false, true, false, false)]
    [InlineData(true, false, false, false)]
    [InlineData(true, true, true, false)]
    public void CanShowPrivateWarBesiegeOption_RequiresEnemyHealthyPartyAndFreeSettlement(
        bool arePrivateEnemies,
        bool hasHealthyMembers,
        bool isUnderSiege,
        bool expected)
    {
        Assert.Equal(
            expected,
            _policy.CanShowPrivateWarBesiegeOption(arePrivateEnemies, hasHealthyMembers, isUnderSiege));
    }

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    public void CanShowPrivateWarContinueSiegeOption_RequiresEnemyAndVanillaSiegeShape(
        bool arePrivateEnemies,
        bool hasVanillaSiegeShape,
        bool expected)
    {
        Assert.Equal(
            expected,
            _policy.CanShowPrivateWarContinueSiegeOption(arePrivateEnemies, hasVanillaSiegeShape));
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void CanShowPrivateWarArmyAttackOption_RequiresPrivateEnemy(
        bool arePrivateEnemies,
        bool expected)
    {
        Assert.Equal(expected, _policy.CanShowPrivateWarArmyAttackOption(arePrivateEnemies));
    }
}
