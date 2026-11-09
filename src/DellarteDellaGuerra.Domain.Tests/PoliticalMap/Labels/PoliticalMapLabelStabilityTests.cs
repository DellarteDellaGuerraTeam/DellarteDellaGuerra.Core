using DellarteDellaGuerra.Domain.PoliticalMap.Labels;
using DellarteDellaGuerra.Domain.Titles.Model;

namespace DellarteDellaGuerra.Domain.Tests.PoliticalMap.Labels;

public class PoliticalMapLabelStabilityTests
{
    [Fact]
    public void ACrowdedLabelCanUseASafeAnchorWithoutChangingItsIdentityOrSize()
    {
        var first = new PoliticalMapLabelCandidate(1, 500, 300, 50, 10, 2, SizeIndex: 2);
        var second = new PoliticalMapLabelCandidate(2, 500, 300, 50, 10, 1, SizeIndex: 2,
            AlternativeAnchors: new[] { new PoliticalMapLabelAnchor(500, 360) });
        var placed = PoliticalMapLabelLayout.Place(new[] { first, second }, 1920, 1080);
        Assert.Equal(2, placed.Count);
        Assert.Equal(new PoliticalMapPlacedLabel(2, 500, 360, 2), placed[1]);
    }

    [Fact]
    public void WrappedNamesCanUseTheEmptySpaceBesideAShortLine()
    {
        var title = new PoliticalMapLabelCandidate(1, 500, 300, 50, 14, 2, "Duchy", LineCount: 3,
            SizeIndex: 2, LineWidths: new[] { 2f, 1f, 7f });
        var neighbour = new PoliticalMapLabelCandidate(2, 565, 280, 50, 4, 1, "Earldom", SizeIndex: 2,
            LineWidths: new[] { 2f });
        Assert.Equal(2, PoliticalMapLabelLayout.Place(new[] { title, neighbour }, 1920, 1080).Count);
        Assert.Single(PoliticalMapLabelLayout.Place(new[] { title, neighbour with { Y = 320 } }, 1920, 1080));
    }

    [Fact]
    public void MeasuredGlyphWidthsReplaceTheConservativeCharacterCount()
    {
        var first = new PoliticalMapLabelCandidate(1, 500, 300, 50, 12, 2, SizeIndex: 2,
            LineWidths: new[] { 4f });
        var second = new PoliticalMapLabelCandidate(2, 600, 300, 50, 12, 1, SizeIndex: 2,
            LineWidths: new[] { 4f });
        Assert.Equal(2, PoliticalMapLabelLayout.Place(new[] { first, second }, 1920, 1080).Count);
        Assert.Single(PoliticalMapLabelLayout.Place(new[] { first, second with { X = 580 } }, 1920, 1080));
    }

    [Fact]
    public void CrowdingThePreferredPatchDoesNotMoveItsNameToANearbyBarony()
    {
        foreach (float separation in new[] { 70f, 90f, 110f })
        {
            var placed = PoliticalMapLabelLayout.Place(new[]
            {
                new PoliticalMapLabelCandidate(1, 500, 300, 50, 10, 3, "Duchy", SizeIndex: 2),
                new PoliticalMapLabelCandidate(2, 500 + separation, 300, 50, 10, 2, "Shrewsbury", SizeIndex: 2),
                new PoliticalMapLabelCandidate(3, 750 + separation, 300, 50, 10, 1, "Shrewsbury", SizeIndex: 2)
            }, 1920, 1080, includeOffscreen: true);

            Assert.DoesNotContain(placed, label => label.Id == 3);
            Assert.Equal(separation >= 110, placed.Any(label => label.Id == 2));
        }
    }

    [Fact]
    public void ADistantPatchCanStillHaveItsOwnNameWhenThePreferredPatchIsCrowded()
    {
        var placed = PoliticalMapLabelLayout.Place(new[]
        {
            new PoliticalMapLabelCandidate(1, 300, 300, 50, 10, 3, "Duchy", SizeIndex: 2),
            new PoliticalMapLabelCandidate(2, 300, 300, 50, 10, 2, "Shrewsbury", SizeIndex: 2),
            new PoliticalMapLabelCandidate(3, 1200, 300, 50, 10, 1, "Shrewsbury", SizeIndex: 2)
        }, 1920, 1080, includeOffscreen: true);
        Assert.Equal(new[] { 1, 3 }, placed.Select(label => label.Id));
    }

    [Fact]
    public void ATitleCrossingTheScreenEdgeCannotPromoteANearbyBarony()
    {
        foreach (float pan in new[] { -600f, -300f, -150f, 0f, 200f, 400f, 900f })
        {
            var placed = PoliticalMapLabelLayout.Place(new[]
            {
                new PoliticalMapLabelCandidate(1, 100 + pan, 300, 50, 12, 2, "Earldom"),
                new PoliticalMapLabelCandidate(2, 150 + pan, 300, 50, 8, 1, "Barony")
            }, 800, 600, includeOffscreen: true);
            Assert.Equal(1, Assert.Single(placed).Id);
        }
    }

    [Fact]
    public void RepeatedPatchesDoNotSwapNamesWhenTheirPreferredPatchLeavesTheScreen()
    {
        foreach (float pan in new[] { -600f, -200f, 0f, 400f, 1000f })
        {
            var placed = PoliticalMapLabelLayout.Place(new[]
            {
                new PoliticalMapLabelCandidate(1, 100 + pan, 300, 50, 12, 2, "Devon"),
                new PoliticalMapLabelCandidate(2, 250 + pan, 300, 50, 12, 1, "Devon")
            }, 800, 600, includeOffscreen: true);
            Assert.Equal(1, Assert.Single(placed).Id);
        }
    }

    [Fact]
    public void SmallCameraOscillationsDoNotFlashACollidingLabelBackOn()
    {
        var shown = new HashSet<int> { 1, 2 };
        int[] Frame(float x)
        {
            var placed = PoliticalMapLabelLayout.Place(new[]
            {
                new PoliticalMapLabelCandidate(1, 500, 500, 40, 8, 2, SizeIndex: 2),
                new PoliticalMapLabelCandidate(2, x, 500, 40, 8, 1, SizeIndex: 2)
            }, 1920, 1080, shown);
            shown = placed.Select(x => x.Id).ToHashSet();
            return shown.OrderBy(x => x).ToArray();
        }

        // Each label is 88 px wide including the collision gap.
        Assert.Equal(new[] { 1 }, Frame(587));
        foreach (float x in new[] { 589f, 587f, 591f, 586f, 590f })
            Assert.Equal(new[] { 1 }, Frame(x));
        Assert.Equal(new[] { 1, 2 }, Frame(605));
        Assert.Equal(new[] { 1, 2 }, Frame(589));
    }

    [Fact]
    public void RepeatSuppressionHasTheSameReentryTolerance()
    {
        var shown = new HashSet<int> { 1 };
        PoliticalMapLabelCandidate[] At(float x) => new[]
        {
            new PoliticalMapLabelCandidate(1, 300, 500, 40, 8, 2, "Devon", SizeIndex: 2),
            new PoliticalMapLabelCandidate(2, x, 500, 40, 8, 1, "Devon", SizeIndex: 2)
        };
        Assert.Single(PoliticalMapLabelLayout.Place(At(1029), 1920, 1080, shown));
        Assert.Equal(2, PoliticalMapLabelLayout.Place(At(1045), 1920, 1080, shown).Count);
    }

    [Fact]
    public void StabilityNeverLetsAnOldBaronySuppressANewDuchy()
    {
        var placed = PoliticalMapLabelLayout.Place(new[]
        {
            new PoliticalMapLabelCandidate(1, 500, 500, 40, 8, 1),
            new PoliticalMapLabelCandidate(2, 500, 500, 40, 8, 2)
        }, 1920, 1080, new HashSet<int> { 1 });
        Assert.Equal(2, Assert.Single(placed).Id);
    }

    [Fact]
    public void FontAndMinimumVisibilityThresholdsHaveToleranceInBothDirections()
    {
        int? size = 2; // size 20, next step 25 at radius 31.25.
        foreach (float radius in new[] { 31.3f, 31f, 31.5f, 30.9f })
        {
            size = PoliticalMapLabelHierarchy.StableSizeIndexFor(PoliticalMapLabelTier.Owner, null, radius, size);
            Assert.Equal(2, size);
        }
        Assert.Equal(3, PoliticalMapLabelHierarchy.StableSizeIndexFor(PoliticalMapLabelTier.Owner, null, 35, size));
        Assert.Equal(0, PoliticalMapLabelHierarchy.StableSizeIndexFor(PoliticalMapLabelTier.Territory, TitleRank.Baron, 16, 0));
        Assert.Equal(-1, PoliticalMapLabelHierarchy.StableSizeIndexFor(PoliticalMapLabelTier.Territory, TitleRank.Baron, 14, 0));
        Assert.Equal(-1, PoliticalMapLabelHierarchy.StableSizeIndexFor(PoliticalMapLabelTier.Territory, TitleRank.Baron, 16.5f, -1));
        Assert.Equal(0, PoliticalMapLabelHierarchy.StableSizeIndexFor(PoliticalMapLabelTier.Territory, TitleRank.Baron, 18, -1));
    }

    [Fact]
    public void TinyRadiusChangesDoNotRewrapTheTextAndChangeItsCollisionBox()
    {
        Assert.Equal(12, PoliticalMapLabelWrapping.TargetLengthFor(65, 2, 12));
        Assert.Equal(13, PoliticalMapLabelWrapping.TargetLengthFor(60, 2, 13));
        Assert.Equal(16, PoliticalMapLabelWrapping.TargetLengthFor(80, 2, 12));
    }
}
