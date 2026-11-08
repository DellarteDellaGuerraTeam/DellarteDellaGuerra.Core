using DellarteDellaGuerra.Domain.Combat;

namespace DellarteDellaGuerra.Domain.Tests.Combat;

public class CombatBalanceTallyTests
{
    [Fact]
    public void BlowsOfTheSameGroup_AreAveragedOnOneRow()
    {
        var tally = new CombatBalanceTally();

        tally.Add("baselard T2 Cut vs Plate", 40, 60f, 6, false);
        tally.Add("baselard T2 Cut vs Plate", 50, 70f, 11, true);

        var row = Assert.Single(tally.Report());
        Assert.Equal("baselard T2 Cut vs Plate: hits=2 raw=45.0 armour=65.0 damage=8.5 max=11 kills=1", row);
    }

    [Fact]
    public void AnotherGroup_GetsItsOwnRow()
    {
        var tally = new CombatBalanceTally();

        tally.Add("sallet Plate vs Cut", 40, 16f, 30, false);
        tally.Add("sallet Plate vs Pierce", 40, 16f, 30, false);

        Assert.Equal(2, tally.Report().Count);
    }
}
