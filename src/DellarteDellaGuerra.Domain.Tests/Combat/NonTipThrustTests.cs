using DellarteDellaGuerra.Domain.Combat;

namespace DellarteDellaGuerra.Domain.Tests.Combat;

public class NonTipThrustTests
{
    private const float VanillaMultiplier = 0.15f;

    [Fact]
    public void ASpearKeepsMoreOfAShaftHitThanAPike()
    {
        Assert.Equal(0.45f, ShippedCombatRules.NonTipThrust.MagnitudeScale("09_ddg_lance") * VanillaMultiplier, 3);
        Assert.Equal(0.3f, ShippedCombatRules.NonTipThrust.MagnitudeScale("10_Pike") * VanillaMultiplier, 3);
    }

    [Theory]
    [InlineData("04_ddg_longsword_template")]
    [InlineData("07_ddg_pollaxe")]
    [InlineData("08_ddg_halberd")]
    [InlineData(null)]
    public void OtherWeaponsKeepTheVanillaPenalty(string? template)
    {
        Assert.Equal(1f, ShippedCombatRules.NonTipThrust.MagnitudeScale(template));
    }
}
