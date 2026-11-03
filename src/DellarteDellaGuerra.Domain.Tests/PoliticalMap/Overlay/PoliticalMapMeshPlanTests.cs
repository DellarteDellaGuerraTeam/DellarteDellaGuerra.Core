using DellarteDellaGuerra.Domain.PoliticalMap.Overlay;

namespace DellarteDellaGuerra.Domain.Tests.PoliticalMap.Overlay;

public class PoliticalMapMeshPlanTests
{
    private static PoliticalMapSettlementPaint Paint(uint colour) => new(colour, 0, null, null, null);
    private static PoliticalMapMeshPlan Plan() => new(new[] { "a", "b", "c" }, new[] { (0, 1) });

    [Fact]
    public void AnUnchangedCachedLevelStaysReadyAndLabelChangesDoNotRebuildMeshes()
    {
        var plan = Plan();
        var paints = new[] { Paint(1), Paint(2), Paint(3) };
        Assert.False(plan.IsReady);
        Assert.True(plan.Update(paints));
        var request = plan.Capture();
        Assert.Equal(new[] { "a", "b", "c" }, request.Territories.OrderBy(t => t));
        Assert.True(plan.Complete(request));
        Assert.True(plan.IsReady);

        paints[0] = paints[0] with { CountyKey = "renamed" };
        Assert.False(plan.Update(paints));
        Assert.True(plan.IsReady);
        Assert.Empty(plan.Capture().Territories);
    }

    [Fact]
    public void EqualColourHierarchyChangesRebuildBothSidesAndTheRestOfTheirDashChains()
    {
        var plan = new PoliticalMapMeshPlan(new[] { "a", "b", "c", "d", "interior", "remote" }, new[] { (0, 1), (2, 3), (3, 4) });
        var paints = new[]
        {
            Paint(1) with { RegionKey = "duke", ParentRegionKey = "duke" },
            Paint(1) with { RegionKey = "earl", ParentRegionKey = "duke" },
            Paint(1) with { RegionKey = "duke", ParentRegionKey = "duke" },
            Paint(1) with { RegionKey = "earl", ParentRegionKey = "duke" },
            Paint(1) with { RegionKey = "earl", ParentRegionKey = "duke" },
            Paint(1) with { RegionKey = "unrelated", ParentRegionKey = "unrelated" }
        };
        plan.Update(paints);
        var old = plan.Capture();
        plan.Complete(old);

        paints[0] = paints[0] with { ParentRegionKey = "other" };
        Assert.True(plan.Update(paints));
        Assert.False(plan.IsCurrent(old));
        Assert.Equal(new[] { "a", "b", "c", "d" }, plan.Capture().Territories.OrderBy(x => x));
        plan.Complete(plan.Capture());

        paints[1] = paints[1] with { RegionKey = "new_earl" };
        Assert.True(plan.Update(paints));
        Assert.Equal(new[] { "a", "b", "c", "d" }, plan.Capture().Territories.OrderBy(x => x));
    }

    [Fact]
    public void AnOwnershipChangeInvalidatesItsSmoothingNeighboursAndRejectsOldWorkerResults()
    {
        var plan = Plan();
        var paints = new[] { Paint(1), Paint(2), Paint(3) };
        plan.Update(paints);
        var old = plan.Capture();
        paints[0] = Paint(7);
        plan.Update(paints);
        Assert.False(plan.IsCurrent(old));
        Assert.False(plan.Complete(old));
        Assert.False(plan.IsReady);
        // An unfinished batch remains owed, even when a later change touches only one county.
        Assert.Equal(new[] { "a", "b", "c" }, plan.Capture().Territories.OrderBy(t => t));
        Assert.Equal(1u, old.Paints[0].Fill);

        Assert.True(plan.Complete(plan.Capture()));
        paints[0] = Paint(8);
        plan.Update(paints);
        Assert.Equal(new[] { "a", "b" }, plan.Capture().Territories.OrderBy(t => t));
    }

    [Fact]
    public void PaintAndHatchSnapshotsAreIndependentFromTheCallersMutableArray()
    {
        var plan = Plan();
        var paints = new[] { Paint(1), Paint(2), Paint(3) };
        plan.Update(paints);
        var request = plan.Capture();
        plan.Complete(request);
        paints[2] = paints[2] with { Hatch = 99 };
        Assert.True(plan.Update(paints));
        Assert.Equal(0u, request.Paints[2].Hatch);
        Assert.Equal(new[] { "c" }, plan.Capture().Territories);
    }

    [Fact]
    public void AChangedColourInvalidatesACacheEvenWhenItReturnsToItsEarlierColour()
    {
        var plan = Plan();
        var paints = new[] { Paint(1), Paint(2), Paint(3) };
        plan.Update(paints);
        var first = plan.Capture();
        paints[0] = Paint(7);
        plan.Update(paints);
        paints[0] = Paint(1);
        plan.Update(paints);
        Assert.False(plan.IsCurrent(first));
        Assert.False(plan.Complete(first));
        Assert.False(plan.IsReady);
    }
}
