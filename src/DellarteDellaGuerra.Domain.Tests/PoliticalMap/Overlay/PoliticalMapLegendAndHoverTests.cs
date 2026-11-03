using DellarteDellaGuerra.Domain.PoliticalMap;
using DellarteDellaGuerra.Domain.PoliticalMap.Overlay;
using DellarteDellaGuerra.Domain.Tests.Titles;
using DellarteDellaGuerra.Domain.Titles.Model;

namespace DellarteDellaGuerra.Domain.Tests.PoliticalMap.Overlay
{
    public class PoliticalMapLegendAndHoverTests
    {
        private readonly FakeWorld _world = new();

        private readonly FakeGenealogy _genealogy = new FakeGenealogy()
            .AddHero("stanley", "clan_stanley")
            .AddHero("somerset", "clan_beaufort");

        private readonly FakeFeudalStructure _structure = new FakeFeudalStructure()
            .AddTitle("duchy_lancaster", TitleRank.Duke, null, "castle_pontefract", "Duchy of Lancaster")
            .AddTitle("county_lancashire", TitleRank.Earl, "duchy_lancaster", "town_lancaster", "Lancashire")
            .AddTitle("county_kent", TitleRank.Earl, null, "town_canterbury", "Kent");

        private readonly FakeTitleRepository _titles = new(
            new Title("duchy_lancaster", "Duchy of Lancaster", TitleRank.Duke, "castle_pontefract", "somerset"),
            new Title("county_lancashire", "Lancashire", TitleRank.Earl, "town_lancaster", "stanley"),
            new Title("county_kent", "Kent", TitleRank.Earl, "town_canterbury", null));

        // The kingdom view's painted map colours: deliberately not the fake world's banner colours.
        private readonly Dictionary<string, uint> _mapColours = new()
        {
            ["kingdom_lancaster"] = 0xFFC0392B,
            ["kingdom_york"] = 0xFF8E44AD,
            ["clan_neville"] = 0xFF16A085,
            ["clan_nameless"] = 0xFF2980B9
        };

        private PoliticalMapHoverDescriber Describer() =>
            new(new PoliticalMapPainter(_structure, _titles, _genealogy, new PoliticalMapTerritories(_structure, _titles)));

        [Fact]
        public void TheLegendListsEachFactionHoldingLandLargestFirstInItsPaintedMapColour()
        {
            IReadOnlyList<PoliticalMapLegendEntry> legend = PoliticalMapLegend.KingdomEntries(
                new[] { "town_lancaster", "village_lancaster", "town_canterbury", "castle_lonely", "town_york" }, _world, _world, _mapColours);

            Assert.Equal(
                new[]
                {
                    new PoliticalMapLegendEntry("kingdom_lancaster", "House of Lancaster", 0xFFC0392B, 2),
                    new PoliticalMapLegendEntry("kingdom_york", "House of York", 0xFF8E44AD, 1),
                    new PoliticalMapLegendEntry("clan_neville", "Neville", 0xFF16A085, 1)
                },
                legend);
        }

        [Fact]
        public void ALegendFactionWithoutANameIsListedUnderItsIdAndOneWithoutAColourIsLeftOut()
        {
            _world.Factions["town_canterbury"] = "clan_nameless";
            _world.Factions["town_york"] = "clan_colourless";

            IReadOnlyList<PoliticalMapLegendEntry> legend = PoliticalMapLegend.KingdomEntries(
                new[] { "town_canterbury", "town_york" }, _world, _world, _mapColours);

            Assert.Equal("clan_nameless", Assert.Single(legend).Name);
        }

        [Fact]
        public void TheLegendIsEmptyUntilTheKingdomViewHasBeenPainted()
        {
            IReadOnlyList<PoliticalMapLegendEntry> legend = PoliticalMapLegend.KingdomEntries(
                new[] { "town_lancaster", "town_york" }, _world, _world, new Dictionary<string, uint>());

            Assert.Empty(legend);
        }

        [Fact]
        public void TheKingdomViewReadoutNamesTheSettlementAndItsFaction()
        {
            PoliticalMapHoverInfo? info = Describer().Describe(PoliticalMapView.Kingdom, "village_lancaster", _world, _world);

            Assert.Equal(new PoliticalMapHoverInfo(PoliticalMapView.Kingdom, "Lancaster Fields", FactionName: "House of Lancaster"), info);
        }

        [Fact]
        public void TheOwnersViewReadoutNamesTheOwnerClanItsHighestTitleAndItsLiege()
        {
            PoliticalMapHoverInfo? vassal = Describer().Describe(PoliticalMapView.Owners, "castle_lonely", _world, _world);
            PoliticalMapHoverInfo? duke = Describer().Describe(PoliticalMapView.Owners, "village_lancaster", _world, _world);

            Assert.Equal(
                new PoliticalMapHoverInfo(PoliticalMapView.Owners, "castle_lonely", OwnerClanName: "Stanley", TitleName: "Lancashire", LiegeClanName: "Beaufort"),
                vassal);
            Assert.Equal(
                new PoliticalMapHoverInfo(
                    PoliticalMapView.Owners, "Lancaster Fields", OwnerClanName: "Beaufort", TitleName: "Duchy of Lancaster", LiegeClanName: "Lancaster"),
                duke);
        }

        [Fact]
        public void AnOwnerWithNoTitleAndNoKingdomHasOnlyItsName()
        {
            PoliticalMapHoverInfo? info = Describer().Describe(PoliticalMapView.Owners, "town_canterbury", _world, _world);

            Assert.Equal(new PoliticalMapHoverInfo(PoliticalMapView.Owners, "Canterbury", OwnerClanName: "Neville"), info);
        }

        [Fact]
        public void LandWithNoOwnerHasOnlyItsSettlementInTheOwnersReadout()
        {
            PoliticalMapHoverInfo? info = Describer().Describe(PoliticalMapView.Owners, "town_york", _world, _world);

            Assert.Equal(new PoliticalMapHoverInfo(PoliticalMapView.Owners, "town_york"), info);
        }

        [Fact]
        public void TheOffViewHasNoReadout()
        {
            Assert.Null(Describer().Describe(PoliticalMapView.Off, "town_lancaster", _world, _world));
        }

        [Theory]
        [InlineData(PoliticalMapView.Kingdom, 0.9f, 0f)]
        [InlineData(PoliticalMapView.Owners, -0.2f, -0.2f)]
        [InlineData(PoliticalMapView.Off, 0.9f, 0.9f)]
        public void SnowIsCappedAtMidsummerWhileTheOverlayIsDrawn(PoliticalMapView view, float season, float drawn)
        {
            Assert.Equal(drawn, new PoliticalMapSnow().SeasonFactorFor(view, season));
        }

        [Fact]
        public void SnowCanBeLeftToTheSeasonOrCappedLower()
        {
            Assert.Equal(0.9f, new PoliticalMapSnow { HideWhileShown = false }.SeasonFactorFor(PoliticalMapView.Kingdom, 0.9f));
            Assert.Equal(-1f, new PoliticalMapSnow { SeasonFactor = -1f }.SeasonFactorFor(PoliticalMapView.Kingdom, 0.9f));
        }

        private sealed class FakeWorld : IPoliticalMapWorld, IPoliticalMapNames
        {
            public readonly Dictionary<string, string> Factions = new()
            {
                ["town_lancaster"] = "kingdom_lancaster",
                ["village_lancaster"] = "kingdom_lancaster",
                ["town_canterbury"] = "clan_neville",
                ["town_york"] = "kingdom_york"
            };

            private readonly Dictionary<string, uint> _colours = new()
            {
                ["kingdom_lancaster"] = 0xFFD41E1E,
                ["kingdom_york"] = 0xFF7B1FA2,
                ["clan_neville"] = 0xFF555555,
                ["clan_nameless"] = 0xFF123456
            };

            private readonly Dictionary<string, string> _names = new()
            {
                ["town_lancaster"] = "Lancaster",
                ["village_lancaster"] = "Lancaster Fields",
                ["town_canterbury"] = "Canterbury",
                ["kingdom_lancaster"] = "House of Lancaster",
                ["kingdom_york"] = "House of York",
                ["clan_neville"] = "Neville",
                ["clan_beaufort"] = "Beaufort",
                ["clan_stanley"] = "Stanley",
                ["clan_lancaster"] = "Lancaster",
                ["clan_colourless"] = "Colourless"
            };

            public string? SeatOf(string settlementId) => settlementId == "village_lancaster" ? "town_lancaster" : settlementId;

            public string? OwnerClanOf(string settlementId) => SeatOf(settlementId) switch
            {
                "town_lancaster" => "clan_beaufort",
                "town_canterbury" => "clan_neville",
                "castle_lonely" => "clan_stanley",
                _ => null
            };

            public string? FactionOf(string settlementId) => Factions.TryGetValue(settlementId, out string? faction) ? faction : null;

            public uint? ClanColor(string clanId) => _colours.TryGetValue(clanId, out uint colour) ? colour : null;

            public uint? FactionColor(string factionId) => ClanColor(factionId);

            public string? KingdomOfClan(string clanId) =>
                clanId is "clan_lancaster" or "clan_beaufort" or "clan_stanley" ? "kingdom_lancaster" : null;

            public string? RulerClanOf(string kingdomId) => kingdomId == "kingdom_lancaster" ? "clan_lancaster" : null;

            public string? SettlementName(string settlementId) => NameOf(settlementId);

            public string? FactionName(string factionId) => NameOf(factionId);

            public string? ClanName(string clanId) => NameOf(clanId);

            private string? NameOf(string id) => _names.TryGetValue(id, out string? name) ? name : null;
        }
    }
}
