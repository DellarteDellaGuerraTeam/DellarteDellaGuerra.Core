using DellarteDellaGuerra.Domain.PoliticalMap.Overlay;

namespace DellarteDellaGuerra.Domain.Tests.PoliticalMap.Overlay
{
    public class PoliticalMapViewZoomTests
    {
        [Fact]
        public void TheToggleCyclesThroughEveryViewAndWraps()
        {
            var seen = new List<PoliticalMapView>();
            PoliticalMapView view = PoliticalMapView.Kingdom;
            for (int i = 0; i < 3; i++)
            {
                seen.Add(view);
                view = PoliticalMapViews.Next(view);
            }

            Assert.Equal(
                new[] { PoliticalMapView.Kingdom, PoliticalMapView.Owners, PoliticalMapView.Off },
                seen);
            Assert.Equal(PoliticalMapView.Kingdom, view);
            Assert.Equal(new[] { "Kingdom", "Owners", "Off" }, Enum.GetNames(typeof(PoliticalMapView)));
        }

        [Fact]
        public void AnOldSavedViewNumberFallsBackToAViewThatStillExists()
        {
            // The titles view was 1 and the owners view 2: they now read as owners and off.
            Assert.Equal(PoliticalMapView.Owners, (PoliticalMapView)1);
            Assert.Equal(PoliticalMapView.Off, (PoliticalMapView)2);
            Assert.False(Enum.IsDefined(typeof(PoliticalMapView), 3));
        }

        [Theory]
        [InlineData(200f, PoliticalMapOwnerLevel.SubVassals, PoliticalMapOwnerLevel.Vassals)]
        [InlineData(121f, PoliticalMapOwnerLevel.SubVassals, PoliticalMapOwnerLevel.Vassals)]
        [InlineData(120f, PoliticalMapOwnerLevel.SubVassals, PoliticalMapOwnerLevel.SubVassals)]
        [InlineData(120f, PoliticalMapOwnerLevel.Vassals, PoliticalMapOwnerLevel.Vassals)]
        [InlineData(115f, PoliticalMapOwnerLevel.SubVassals, PoliticalMapOwnerLevel.SubVassals)]
        [InlineData(115f, PoliticalMapOwnerLevel.Vassals, PoliticalMapOwnerLevel.Vassals)]
        [InlineData(110f, PoliticalMapOwnerLevel.SubVassals, PoliticalMapOwnerLevel.SubVassals)]
        [InlineData(110f, PoliticalMapOwnerLevel.Vassals, PoliticalMapOwnerLevel.Vassals)]
        [InlineData(109f, PoliticalMapOwnerLevel.Vassals, PoliticalMapOwnerLevel.SubVassals)]
        [InlineData(5f, PoliticalMapOwnerLevel.Vassals, PoliticalMapOwnerLevel.SubVassals)]
        public void TheOwnersLevelSwitchesFarOutAndBackOnlyPastTheHysteresis(
            float distance, PoliticalMapOwnerLevel current, PoliticalMapOwnerLevel expected)
        {
            Assert.Equal(expected, PoliticalMapZoom.OwnerLevelFor(distance, current));
        }

        [Theory]
        [InlineData(5f, PoliticalMapZoomBand.Near)]
        [InlineData(38f, PoliticalMapZoomBand.Mid)]
        [InlineData(200f, PoliticalMapZoomBand.Far)]
        public void TheCameraDistancePicksTheZoomBand(float distance, PoliticalMapZoomBand band)
        {
            Assert.Equal(band, PoliticalMapZoom.BandOf(distance));
        }

        [Fact]
        public void CloseInTheWashIsHiddenAndFurtherOutItShows()
        {
            Assert.False(PoliticalMapZoom.ShowsWash(PoliticalMapZoomBand.Near));
            Assert.True(PoliticalMapZoom.ShowsWash(PoliticalMapZoomBand.Mid));
            Assert.True(PoliticalMapZoom.ShowsWash(PoliticalMapZoomBand.Far));
        }
    }
}
