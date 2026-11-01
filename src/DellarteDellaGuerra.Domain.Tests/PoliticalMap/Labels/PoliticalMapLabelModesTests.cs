using DellarteDellaGuerra.Domain.PoliticalMap.Labels;
using DellarteDellaGuerra.Domain.PoliticalMap.Overlay;

namespace DellarteDellaGuerra.Domain.Tests.PoliticalMap.Labels
{
    public class PoliticalMapLabelModesTests
    {
        [Fact]
        public void TheOwnersViewStartsOnClanNames()
        {
            Assert.Equal(PoliticalMapLabelMode.Clans, new PoliticalMapLabelModes().Of(PoliticalMapView.Owners));
        }

        [Fact]
        public void BothMapViewsOfferTheChoice()
        {
            Assert.True(PoliticalMapLabelModes.HasChoice(PoliticalMapView.Owners));
            Assert.True(PoliticalMapLabelModes.HasChoice(PoliticalMapView.Kingdom));
            Assert.False(PoliticalMapLabelModes.HasChoice(PoliticalMapView.Off));
        }

        [Fact]
        public void TheKingdomViewStartsOnTitlesAndKeepsItsOwnChoice()
        {
            var modes = new PoliticalMapLabelModes();
            Assert.Equal(PoliticalMapLabelMode.Titles, modes.Of(PoliticalMapView.Kingdom));

            modes.Toggle(PoliticalMapView.Kingdom);
            Assert.Equal(PoliticalMapLabelMode.Clans, modes.Of(PoliticalMapView.Kingdom));
            modes.Toggle(PoliticalMapView.Owners);
            Assert.Equal(PoliticalMapLabelMode.Titles, modes.Of(PoliticalMapView.Owners));
            Assert.Equal(PoliticalMapLabelMode.Clans, modes.Of(PoliticalMapView.Kingdom));

            modes.Toggle(PoliticalMapView.Kingdom);
            Assert.Equal(PoliticalMapLabelMode.Titles, modes.Of(PoliticalMapView.Kingdom));
            Assert.Equal(PoliticalMapLabelMode.Titles, modes.Of(PoliticalMapView.Owners));
        }

        [Fact]
        public void ToggleFlipsTheOwnersViewAndRaisesChanged()
        {
            var modes = new PoliticalMapLabelModes();
            int changes = 0;
            modes.Changed += () => changes++;

            modes.Toggle(PoliticalMapView.Owners);
            Assert.Equal(PoliticalMapLabelMode.Titles, modes.Of(PoliticalMapView.Owners));
            Assert.Equal(1, changes);

            modes.Toggle(PoliticalMapView.Owners);
            Assert.Equal(PoliticalMapLabelMode.Clans, modes.Of(PoliticalMapView.Owners));
            Assert.Equal(2, changes);
        }

        [Fact]
        public void SettingTheSameModeOrAViewWithoutTheChoiceChangesNothing()
        {
            var modes = new PoliticalMapLabelModes();
            int changes = 0;
            modes.Changed += () => changes++;

            modes.Set(PoliticalMapView.Owners, PoliticalMapLabelMode.Clans);
            modes.Set(PoliticalMapView.Kingdom, PoliticalMapLabelMode.Titles);
            modes.Toggle(PoliticalMapView.Off);

            Assert.Equal(0, changes);
            Assert.Equal(PoliticalMapLabelMode.Titles, modes.Of(PoliticalMapView.Kingdom));
        }

        [Theory]
        [InlineData("title", PoliticalMapLabelMode.Titles)]
        [InlineData("Titles", PoliticalMapLabelMode.Titles)]
        [InlineData("CLAN", PoliticalMapLabelMode.Clans)]
        [InlineData(" clans ", PoliticalMapLabelMode.Clans)]
        public void ParsesTitleOrClan(string text, PoliticalMapLabelMode expected)
        {
            Assert.True(PoliticalMapLabelModes.TryParse(text, out PoliticalMapLabelMode mode));
            Assert.Equal(expected, mode);
        }

        [Fact]
        public void RejectsAnythingElse()
        {
            Assert.False(PoliticalMapLabelModes.TryParse("duchy", out _));
            Assert.False(PoliticalMapLabelModes.TryParse(null, out _));
        }
    }
}
