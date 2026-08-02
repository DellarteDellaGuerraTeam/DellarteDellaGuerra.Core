using Bannerlord.PrivateWars.Domain.Siege;

namespace DellarteDellaGuerra.Domain.Tests.PrivateWars;

public class PrivateWarSallyOutPolicyTests
{
    private readonly SallyOutPolicy _policy = new();

    [Theory]
    [InlineData(false, true, false, true, true, true, SallyOutPartySide.Besieger)]
    [InlineData(true, false, false, true, false, true, SallyOutPartySide.Besieger)]
    [InlineData(false, false, true, true, true, true, SallyOutPartySide.Settlement)]
    [InlineData(false, false, true, true, true, false, SallyOutPartySide.None)]
    [InlineData(false, false, false, true, true, true, SallyOutPartySide.None)]
    [InlineData(true, false, false, false, false, false, SallyOutPartySide.Besieger)]
    [InlineData(false, false, false, false, true, true, SallyOutPartySide.Settlement)]
    public void ClassifyNearbyParty_UsesPrivateWarSidesOnlyDuringPrivateSieges(
        bool vanillaEnemy,
        bool privateEnemy,
        bool samePrivateWarSide,
        bool isPrivateWarSiege,
        bool sameMapFaction,
        bool matchesNavalContext,
        SallyOutPartySide expected)
    {
        var result = _policy.ClassifyNearbyParty(
            vanillaEnemy,
            privateEnemy,
            samePrivateWarSide,
            isPrivateWarSiege,
            sameMapFaction,
            matchesNavalContext);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(201f, 100f, false, true)]
    [InlineData(200f, 100f, false, false)]
    [InlineData(151f, 100f, true, true)]
    [InlineData(150f, 100f, true, false)]
    public void ShouldSally_PreservesVanillaStrictStrengthRatios(
        float settlementStrength,
        float besiegerStrength,
        bool reliefBattleActive,
        bool expected)
    {
        Assert.Equal(
            expected,
            _policy.ShouldSally(settlementStrength, besiegerStrength, reliefBattleActive));
    }
}
