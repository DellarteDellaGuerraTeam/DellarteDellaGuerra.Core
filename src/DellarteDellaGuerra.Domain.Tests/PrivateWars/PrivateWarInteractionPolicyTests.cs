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
}
