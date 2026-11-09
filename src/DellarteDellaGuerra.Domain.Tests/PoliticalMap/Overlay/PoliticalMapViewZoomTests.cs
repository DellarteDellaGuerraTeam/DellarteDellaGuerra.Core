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
            for (int i = 0; i < 4; i++)
            {
                seen.Add(view);
                view = PoliticalMapViews.Next(view);
            }

            Assert.Equal(
                new[] { PoliticalMapView.Kingdom, PoliticalMapView.Owners, PoliticalMapView.DeJure, PoliticalMapView.Off },
                seen);
            Assert.Equal(PoliticalMapView.Kingdom, view);
            Assert.Equal(new[] { "Kingdom", "Owners", "Off", "DeJure" }, Enum.GetNames(typeof(PoliticalMapView)));
        }

        [Fact]
        public void AnOldSavedViewNumberFallsBackToAViewThatStillExists()
        {
            // The titles view was 1 and the owners view 2: they now read as owners and off.
            Assert.Equal(PoliticalMapView.Owners, (PoliticalMapView)1);
            Assert.Equal(PoliticalMapView.Off, (PoliticalMapView)2);
            Assert.Equal(PoliticalMapView.DeJure, (PoliticalMapView)3);
        }

        [Theory]
        [InlineData(620f, PoliticalMapOwnerLevel.SubVassals, PoliticalMapOwnerLevel.Vassals)]
        [InlineData(261f, PoliticalMapOwnerLevel.SubVassals, PoliticalMapOwnerLevel.Vassals)]
        [InlineData(260f, PoliticalMapOwnerLevel.SubVassals, PoliticalMapOwnerLevel.SubVassals)]
        [InlineData(260f, PoliticalMapOwnerLevel.Vassals, PoliticalMapOwnerLevel.Vassals)]
        [InlineData(255f, PoliticalMapOwnerLevel.SubVassals, PoliticalMapOwnerLevel.SubVassals)]
        [InlineData(255f, PoliticalMapOwnerLevel.Vassals, PoliticalMapOwnerLevel.Vassals)]
        [InlineData(250f, PoliticalMapOwnerLevel.SubVassals, PoliticalMapOwnerLevel.SubVassals)]
        [InlineData(250f, PoliticalMapOwnerLevel.Vassals, PoliticalMapOwnerLevel.Vassals)]
        [InlineData(249f, PoliticalMapOwnerLevel.Vassals, PoliticalMapOwnerLevel.SubVassals)]
        [InlineData(214f, PoliticalMapOwnerLevel.Vassals, PoliticalMapOwnerLevel.SubVassals)]
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
