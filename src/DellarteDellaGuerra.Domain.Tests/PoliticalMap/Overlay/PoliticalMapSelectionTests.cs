using DellarteDellaGuerra.Domain.PoliticalMap.Labels;
using DellarteDellaGuerra.Domain.PoliticalMap.Overlay;

namespace DellarteDellaGuerra.Domain.Tests.PoliticalMap.Overlay
{
    public class PoliticalMapSelectionTests
    {
        private static readonly PoliticalMapSettlementPaint[] Paints =
        {
            new(0x99C86432, 0, "county_a", null, null, "clan_a") { ParentRegionKey = "clan_duke" },
            new(0x991E64DC, 0, "county_b", null, null, "clan_b"),
            new(0x99C86432, 0x99506478, "county_c", null, null, "clan_a") { ParentRegionKey = "clan_duke" },
            PoliticalMapSettlementPaint.Blank
        };

        private static PoliticalMapSelection Select(string key) =>
            new(PoliticalMapView.Owners, PoliticalMapOwnerLevel.Vassals, PoliticalMapLabelMode.Clans, key);

        [Fact]
        public void AllDisconnectedPiecesAreDarkenedWithoutChangingOtherRegionsOrFrontiers()
        {
            var result = Select("clan_a").Apply(Paints, new[] { "clan_a", "clan_b", "clan_a", null });

            Assert.Equal(0xD932190Cu, result[0].Fill);
            Assert.Equal(result[0].Fill, result[2].Fill);
            Assert.Equal(0xD914191Eu, result[2].Hatch);
            Assert.Equal(Paints[1], result[1]);
            Assert.Equal(PoliticalMapSettlementPaint.Blank, result[3]);
            Assert.Equal(Paints[0].FrontierKey, result[0].FrontierKey);
            Assert.Equal(Paints[0].ParentRegionKey, result[0].ParentRegionKey);
            Assert.Equal(Paints[0].CountyKey, result[0].CountyKey);
            Assert.Equal(0x99C86432u, Paints[0].Fill);
        }

        [Fact]
        public void SwitchingSelectionRestoresThePreviousColourAndRepeatedClicksDoNotAccumulateDarkening()
        {
            string?[] keys = { "clan_a", "clan_b", "clan_a", null };
            var a = Select("clan_a").Apply(Paints, keys);
            var b = Select("clan_b").Apply(Paints, keys);

            Assert.Equal(Paints[0], b[0]);
            Assert.Equal(Paints[2], b[2]);
            Assert.NotEqual(Paints[1].Fill, b[1].Fill);
            Assert.Equal(a, Select("clan_a").Apply(Paints, keys));
        }

        [Fact]
        public void SelectionSurvivesZoomButBelongsToItsViewAndLabelMode()
        {
            var selection = Select("clan_a");
            Assert.True(selection.Matches(PoliticalMapView.Owners, PoliticalMapOwnerLevel.Vassals, PoliticalMapLabelMode.Clans));
            Assert.False(selection.Matches(PoliticalMapView.Kingdom, selection.Level, selection.Mode));
            Assert.True(selection.Matches(selection.View, PoliticalMapOwnerLevel.SubVassals, selection.Mode));
            Assert.False(selection.Matches(selection.View, selection.Level, PoliticalMapLabelMode.Titles));
        }

        [Fact]
        public void MissingKeysAreRejectedAndBlankPaintIsNeverMadeVisible()
        {
            Assert.Throws<ArgumentException>(() => Select("clan_a").Apply(Paints, Array.Empty<string?>()));
            Assert.Equal(PoliticalMapSettlementPaint.Blank,
                Select("clan_a").Apply(new[] { PoliticalMapSettlementPaint.Blank }, new[] { "clan_a" })[0]);
        }
    }
}
