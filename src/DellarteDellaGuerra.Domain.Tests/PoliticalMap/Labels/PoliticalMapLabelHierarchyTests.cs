using DellarteDellaGuerra.Domain.PoliticalMap.Labels;
using DellarteDellaGuerra.Domain.PoliticalMap.Overlay;
using DellarteDellaGuerra.Domain.Titles.Model;

namespace DellarteDellaGuerra.Domain.Tests.PoliticalMap.Labels;

public class PoliticalMapLabelHierarchyTests
{
    [Theory]
    [InlineData(TitleRank.King, true)]
    [InlineData(TitleRank.Duke, true)]
    [InlineData(TitleRank.Count, true)]
    [InlineData(TitleRank.Baron, false)]
    [InlineData(null, false)]
    public void ZoomedOutOwnersOnlyLabelTheHigherTitles(TitleRank? rank, bool shown)
    {
        Assert.Equal(shown, PoliticalMapLabelHierarchy.Shows(PoliticalMapLabelTier.Owner, rank, PoliticalMapOwnerLevel.Vassals));
    }

    [Fact]
    public void ZoomingInAddsBaronsAndUntitledEstatesAndKingdomLabelsAreUnaffected()
    {
        Assert.True(PoliticalMapLabelHierarchy.Shows(PoliticalMapLabelTier.Owner, TitleRank.Baron, PoliticalMapOwnerLevel.SubVassals));
        Assert.True(PoliticalMapLabelHierarchy.Shows(PoliticalMapLabelTier.Owner, null, PoliticalMapOwnerLevel.SubVassals));
        Assert.True(PoliticalMapLabelHierarchy.Shows(PoliticalMapLabelTier.Faction, null, PoliticalMapOwnerLevel.Vassals));
    }

    [Fact]
    public void ASmallDuchyWinsACollisionAgainstALargeBarony()
    {
        var labels = PoliticalMapLabelLayout.Place(new[]
        {
            new PoliticalMapLabelCandidate(1, 500, 500, 40, 8,
                PoliticalMapLabelHierarchy.Priority(PoliticalMapLabelTier.Owner, TitleRank.Duke, 200)),
            new PoliticalMapLabelCandidate(2, 500, 500, 80, 10,
                PoliticalMapLabelHierarchy.Priority(PoliticalMapLabelTier.Owner, TitleRank.Baron, 10000))
        }, 1920, 1080);
        Assert.Equal(1, Assert.Single(labels).Id);
    }
}
