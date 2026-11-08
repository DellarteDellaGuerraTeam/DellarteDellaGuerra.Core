using DellarteDellaGuerra.Domain.Combat;

namespace DellarteDellaGuerra.Domain.Tests.Combat;

public class HalfSwordTests
{
    [Fact]
    public void TheHalfSwordGripWeakensSwings()
    {
        Assert.Equal(0.7f, ShippedCombatRules.HalfSword.SwingMagnitudeScale("dadg_Halfsword"));
    }

    [Theory]
    [InlineData("OneHandedSword")]
    [InlineData("TwoHandedSword")]
    [InlineData(null)]
    public void OtherUsagesKeepFullSwings(string? weaponDescription)
    {
        Assert.Equal(1f, ShippedCombatRules.HalfSword.SwingMagnitudeScale(weaponDescription));
    }
}
