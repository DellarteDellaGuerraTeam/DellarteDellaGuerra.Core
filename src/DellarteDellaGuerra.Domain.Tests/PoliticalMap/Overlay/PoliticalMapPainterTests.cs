using DellarteDellaGuerra.Domain.PoliticalMap;
using DellarteDellaGuerra.Domain.PoliticalMap.Labels;
using DellarteDellaGuerra.Domain.PoliticalMap.Overlay;
using DellarteDellaGuerra.Domain.Tests.Titles;
using DellarteDellaGuerra.Domain.Titles.Model;

namespace DellarteDellaGuerra.Domain.Tests.PoliticalMap.Overlay
{
    public class PoliticalMapPainterTests
    {
        private const uint Lancaster = 0xFFAA0000;
        private const uint Beaufort = 0xFF0000AA;
        private const uint Stanley = 0xFF00AA00;
        private const uint Neville = 0xFF555555;
        private const uint KingdomOfLancaster = 0xFFEE1111;

        private static readonly string[] Settlements =
            { "town_lancaster", "village_lancaster", "castle_pontefract", "town_canterbury", "castle_lonely", "town_chester" };

        private readonly FakeFeudalStructure _structure = new FakeFeudalStructure()
            .AddTitle("kingdom_england", TitleRank.King)
            .AddTitle("duchy_lancaster", TitleRank.Duke, "kingdom_england", "castle_pontefract")
            .AddTitle("county_lancashire", TitleRank.Count, "duchy_lancaster", "town_lancaster")
            .AddTitle("county_cheshire", TitleRank.Count, "duchy_lancaster", "town_chester")
            .AddTitle("county_kent", TitleRank.Count, "kingdom_england", "town_canterbury");

        private readonly FakeGenealogy _genealogy = new FakeGenealogy()
            .AddHero("henry", "clan_lancaster")
            .AddHero("somerset", "clan_beaufort")
            .AddHero("stanley", "clan_stanley")
            .AddHero("neville", "clan_neville");

        // Lancaster is king; Beaufort (the duchy) and Neville (Kent) answer to him; Stanley holds
        // two counties under Beaufort's duchy.
        private readonly FakeTitleRepository _titles = new(
            new Title("kingdom_england", "England", TitleRank.King, "", "henry"),
            new Title("duchy_lancaster", "Lancaster", TitleRank.Duke, "castle_pontefract", "somerset"),
            new Title("county_lancashire", "Lancashire", TitleRank.Count, "town_lancaster", "stanley"),
            new Title("county_cheshire", "Cheshire", TitleRank.Count, "town_chester", "stanley"),
            new Title("county_kent", "Kent", TitleRank.Count, "town_canterbury", "neville"));

        private readonly FakeWorld _world = new();

        private readonly Dictionary<string, string> _folds = new() { ["duchy_lancaster"] = "county_lancashire" };

        private PoliticalMapPainter Painter() =>
            new(_structure, _titles, _genealogy, new PoliticalMapTerritories(_structure, _titles));

        // Every settlement touches the next one along the list, and Chester touches Lancaster.
        private static readonly (int A, int B)[] Chain = { (0, 1), (1, 2), (2, 3), (3, 4), (0, 5) };

        private PoliticalMapSettlementPaint[] Paint(
            PoliticalMapView view,
            PoliticalMapPainter? painter = null,
            IReadOnlyList<(int A, int B)>? neighbours = null,
            PoliticalMapOwnerLevel level = PoliticalMapOwnerLevel.SubVassals)
        {
            painter ??= Painter();
            string[] territories = Settlements.Select(id => painter.TerritoryOf(id, _world)).ToArray();
            return painter.Paint(view, Settlements, territories, _folds, _world, neighbours ?? Chain, level);
        }

        private static string?[] Regions(IEnumerable<PoliticalMapSettlementPaint> paints) => paints.Select(paint => paint.RegionKey).ToArray();

        private static uint Wash(uint? argb) => PoliticalMapPalette.Wash(argb);

        private static uint Opaque(uint washed) => washed | 0xFF000000u;

        [Fact]
        public void ASettlementIsMeshedWithItsSeatsTerritoryOrItsSeatWhenThatHoldsNoTitle()
        {
            PoliticalMapPainter painter = Painter();

            Assert.Equal("county_lancashire", painter.TerritoryOf("village_lancaster", _world));
            Assert.Equal("duchy_lancaster", painter.TerritoryOf("castle_pontefract", _world));
            Assert.Equal("settlement:castle_lonely", painter.TerritoryOf("castle_lonely", _world));
        }

        [Fact]
        public void ZoomedOutTheOwnersViewShowsTheRulersLandAndOneRegionPerDirectVassal()
        {
            PoliticalMapSettlementPaint[] paints = Paint(PoliticalMapView.Owners, level: PoliticalMapOwnerLevel.Vassals);

            // Stanley's land is folded into Beaufort's, the vassal he answers to.
            Assert.Equal(
                new[] { "clan_beaufort", "clan_beaufort", "clan_beaufort", "clan_neville", "clan_lancaster", "clan_beaufort" },
                Regions(paints));
            Assert.Equal("county_lancashire", paints[2].CountyKey);
            Assert.Null(paints[0].DuchyKey);
            Assert.Null(paints[0].RealmKey);
        }

        [Fact]
        public void ZoomedInEachDirectVassalsRegionSplitsIntoTheirOwnLandAndTheirVassals()
        {
            PoliticalMapSettlementPaint[] paints = Paint(PoliticalMapView.Owners, level: PoliticalMapOwnerLevel.SubVassals);

            Assert.Equal(
                new[] { "clan_stanley", "clan_stanley", "clan_beaufort", "clan_neville", "clan_lancaster", "clan_stanley" },
                Regions(paints));
        }

        [Fact]
        public void ZoomedInTheSplitStopsTwoLevelsDown()
        {
            // Tudor holds a barony under Stanley's county: three tiers below the king. His land
            // shows in Beaufort's region far out and in Stanley's close in, never in his own.
            _structure.AddTitle("barony_lathom", TitleRank.Baron, "county_lancashire", "castle_lonely");
            _genealogy.AddHero("tudor", "clan_tudor");
            _titles.SaveTitle(new Title("barony_lathom", "Lathom", TitleRank.Baron, "castle_lonely", "tudor"));
            _world.Owners["castle_lonely"] = "clan_tudor";
            _world.Kingdoms["clan_tudor"] = "kingdom_lancaster";

            Assert.Equal("clan_beaufort", Paint(PoliticalMapView.Owners, level: PoliticalMapOwnerLevel.Vassals)[4].RegionKey);
            Assert.Equal("clan_stanley", Paint(PoliticalMapView.Owners, level: PoliticalMapOwnerLevel.SubVassals)[4].RegionKey);
        }

        [Fact]
        public void TheOwnerLabelRegionsFollowTheLevelAndNotTheLabelMode()
        {
            PoliticalMapPainter painter = Painter();
            string?[] KeysAt(PoliticalMapOwnerLevel level) => PoliticalMapLabelTiers.KeysOf(
                PoliticalMapLabelTier.Owner, Settlements,
                Regions(Paint(PoliticalMapView.Owners, painter, neighbours: Array.Empty<(int A, int B)>(), level: level)), _world);

            string?[] far = KeysAt(PoliticalMapOwnerLevel.Vassals);
            string?[] near = KeysAt(PoliticalMapOwnerLevel.SubVassals);
            Assert.Equal(new[] { "clan_beaufort", "clan_beaufort", "clan_beaufort", "clan_neville", "clan_lancaster", "clan_beaufort" }, far);
            Assert.Equal(new[] { "clan_stanley", "clan_stanley", "clan_beaufort", "clan_neville", "clan_lancaster", "clan_stanley" }, near);

            // The titles/clans toggle names the regions differently; it never regroups them.
            var modes = new PoliticalMapLabelModes();
            modes.Toggle(PoliticalMapView.Owners);
            Assert.Equal(near, KeysAt(PoliticalMapOwnerLevel.SubVassals));
            Assert.Equal(far, KeysAt(PoliticalMapOwnerLevel.Vassals));
        }

        [Fact]
        public void LandWithNoOwnerIsUnfilledInTheOwnersView()
        {
            _world.Owners.Remove("town_canterbury");

            PoliticalMapSettlementPaint paint = Paint(PoliticalMapView.Owners)[3];

            Assert.Equal(0u, paint.Fill);
            Assert.Null(paint.RegionKey);
            Assert.Equal("county_kent", paint.CountyKey);
        }

        [Theory]
        [InlineData(PoliticalMapOwnerLevel.Vassals)]
        [InlineData(PoliticalMapOwnerLevel.SubVassals)]
        public void EachOwnersRegionIsOneShadeOfItsKingdomAndTouchingRegionsDiffer(PoliticalMapOwnerLevel level)
        {
            PoliticalMapSettlementPaint[] paints = Paint(PoliticalMapView.Owners, level: level);

            IReadOnlyList<uint> shades = PoliticalMapColouring.Shades(KingdomOfLancaster);
            Assert.All(paints, paint => Assert.Contains(Opaque(paint.Fill), shades));
            Assert.All(paints, paint => Assert.Equal(0u, paint.Hatch));
            foreach (var group in paints.GroupBy(paint => paint.RegionKey)) Assert.Single(group.Select(paint => paint.Fill).Distinct());
            foreach ((int a, int b) in Chain)
            {
                if (paints[a].RegionKey == paints[b].RegionKey) continue;
                Assert.True(PoliticalMapColouring.Distance(paints[a].Fill, paints[b].Fill) >= PoliticalMapColouring.ShadeMinDistance);
            }
        }

        [Fact]
        public void TheOwnersViewIsTheSameForEveryFreshPainterAndKeepsItsColoursOnRepaint()
        {
            PoliticalMapPainter painter = Painter();
            PoliticalMapSettlementPaint[] first = Paint(PoliticalMapView.Owners, painter);

            Assert.Equal(first, Paint(PoliticalMapView.Owners));
            Assert.Equal(first, Paint(PoliticalMapView.Owners, painter));
        }

        [Fact]
        public void EachLevelKeepsItsOwnColoursWhenTheZoomGoesBackAndForth()
        {
            PoliticalMapPainter painter = Painter();
            PoliticalMapSettlementPaint[] far = Paint(PoliticalMapView.Owners, painter, level: PoliticalMapOwnerLevel.Vassals);
            PoliticalMapSettlementPaint[] near = Paint(PoliticalMapView.Owners, painter, level: PoliticalMapOwnerLevel.SubVassals);

            Assert.Equal(far, Paint(PoliticalMapView.Owners, painter, level: PoliticalMapOwnerLevel.Vassals));
            Assert.Equal(near, Paint(PoliticalMapView.Owners, painter, level: PoliticalMapOwnerLevel.SubVassals));
        }

        [Fact]
        public void ZoomingOutAndBackChangesThePaintOnlyOutsideTheHysteresisBand()
        {
            PoliticalMapPainter painter = Painter();
            PoliticalMapOwnerLevel level = PoliticalMapOwnerLevel.SubVassals;
            string?[] PaintAt(float distance)
            {
                level = PoliticalMapZoom.OwnerLevelFor(distance, level);
                return Regions(Paint(PoliticalMapView.Owners, painter, level: level));
            }

            string?[] near = PaintAt(109f);
            string?[] far = PaintAt(121f);
            Assert.Equal("clan_stanley", near[0]);
            Assert.Equal("clan_beaufort", far[0]);
            Assert.Equal("clan_lancaster", far[4]);
            Assert.Equal(far, PaintAt(115f));
            Assert.Equal(near, PaintAt(109f));
            Assert.Equal(near, PaintAt(115f));
        }

        [Fact]
        public void TheMapsColoursIgnoreTheBannerColours()
        {
            PoliticalMapSettlementPaint[] paints = Paint(PoliticalMapView.Owners);

            Assert.NotEqual(Wash(Stanley), paints[0].Fill);
            Assert.NotEqual(Wash(Neville), paints[3].Fill);
        }

        [Fact]
        public void AnOwnersViewClanUnderNoKingdomIsItsOwnRegionInAKingdomlessColour()
        {
            _world.Kingdoms.Remove("clan_neville");

            PoliticalMapPainter painter = Painter();
            PoliticalMapSettlementPaint[] paints = Paint(PoliticalMapView.Owners, painter, level: PoliticalMapOwnerLevel.Vassals);

            Assert.Equal("clan_neville", paints[3].RegionKey);
            Assert.Contains(Opaque(paints[3].Fill), PoliticalMapColouring.KingdomlessPalette);
            IReadOnlyDictionary<string, uint> legend = painter.MapColours(PoliticalMapView.Owners);
            Assert.Equal(KingdomOfLancaster, legend["kingdom_lancaster"]);
            Assert.Equal(Opaque(paints[3].Fill), legend["clan_neville"]);
            Assert.False(legend.ContainsKey("clan_beaufort"));
        }

        [Fact]
        public void TheKingdomViewPaintsKingdomsInTheirMapColourAndAKingdomlessClanInADistinctOne()
        {
            foreach (string settlement in Settlements) _world.Factions[settlement] = "sturgia";
            _world.Factions["town_canterbury"] = "clan_neville";

            PoliticalMapSettlementPaint[] paints = Paint(PoliticalMapView.Kingdom);

            Assert.Equal(Wash(PoliticalMapPalette.KingdomColor("sturgia", null)), paints[0].Fill);
            Assert.Equal("sturgia", paints[0].RealmKey);
            Assert.Contains(Opaque(paints[3].Fill), PoliticalMapColouring.KingdomlessPalette);
            Assert.True(PoliticalMapColouring.Distance(paints[3].Fill, paints[2].Fill) >= PoliticalMapColouring.DistinctMinDistance);
            Assert.Equal("clan_neville", paints[3].RealmKey);
            Assert.Null(paints[0].DuchyKey);
        }

        [Fact]
        public void APaintWithNeighboursIsRememberedAndKeptOnTheNextPaint()
        {
            PoliticalMapPainter painter = Painter();
            PoliticalMapSettlementPaint[] first = Paint(PoliticalMapView.Owners, painter);

            Assert.Equal(KingdomOfLancaster, painter.MapColours(PoliticalMapView.Owners)["kingdom_lancaster"]);
            Assert.Empty(painter.MapColours(PoliticalMapView.Kingdom));
            Assert.Equal(first, Paint(PoliticalMapView.Owners, painter));
        }

        [Fact]
        public void ThePaintDoesNotDependOnTheOrderTheNeighboursAreListedIn()
        {
            IReadOnlyList<(int A, int B)> reversed = Chain.Reverse().Select(pair => (pair.B, pair.A)).ToArray();

            foreach (PoliticalMapView view in new[] { PoliticalMapView.Owners, PoliticalMapView.Kingdom })
            {
                Assert.Equal(Paint(view), Paint(view, neighbours: reversed));
            }
        }

        [Fact]
        public void APaintWithoutNeighboursLeavesTheLegendColoursAlone()
        {
            PoliticalMapPainter painter = Painter();
            string[] territories = Settlements.Select(id => painter.TerritoryOf(id, _world)).ToArray();

            painter.Paint(PoliticalMapView.Owners, Settlements, territories, _folds, _world);

            Assert.Empty(painter.MapColours(PoliticalMapView.Owners));
        }

        [Fact]
        public void TheOffViewPaintsNothing()
        {
            Assert.All(Paint(PoliticalMapView.Off), paint => Assert.Equal(PoliticalMapSettlementPaint.Blank, paint));
        }

        private sealed class FakeWorld : IPoliticalMapWorld
        {
            public readonly Dictionary<string, string> Owners = new()
            {
                ["town_lancaster"] = "clan_stanley",
                ["castle_pontefract"] = "clan_beaufort",
                ["town_canterbury"] = "clan_neville",
                ["castle_lonely"] = "clan_lancaster",
                ["town_chester"] = "clan_stanley"
            };

            public readonly Dictionary<string, string> Factions = new()
            {
                ["town_lancaster"] = "kingdom_lancaster",
                ["village_lancaster"] = "kingdom_lancaster",
                ["castle_pontefract"] = "kingdom_lancaster",
                ["town_canterbury"] = "kingdom_lancaster",
                ["castle_lonely"] = "kingdom_lancaster",
                ["town_chester"] = "kingdom_lancaster"
            };

            public readonly Dictionary<string, string> Kingdoms = new()
            {
                ["clan_lancaster"] = "kingdom_lancaster",
                ["clan_beaufort"] = "kingdom_lancaster",
                ["clan_stanley"] = "kingdom_lancaster",
                ["clan_neville"] = "kingdom_lancaster"
            };

            private readonly Dictionary<string, uint> _colours = new()
            {
                ["clan_lancaster"] = Lancaster,
                ["clan_beaufort"] = Beaufort,
                ["clan_stanley"] = Stanley,
                ["clan_neville"] = Neville,
                ["kingdom_lancaster"] = KingdomOfLancaster
            };

            public string? SeatOf(string settlementId) => settlementId == "village_lancaster" ? "town_lancaster" : settlementId;

            public string? OwnerClanOf(string settlementId) => Owners.TryGetValue(SeatOf(settlementId)!, out string? clan) ? clan : null;

            public string? FactionOf(string settlementId) => Factions.TryGetValue(settlementId, out string? faction) ? faction : null;

            public uint? ClanColor(string clanId) => _colours.TryGetValue(clanId, out uint colour) ? colour : null;

            public uint? FactionColor(string factionId) => ClanColor(factionId);

            public string? KingdomOfClan(string clanId) => Kingdoms.TryGetValue(clanId, out string? kingdom) ? kingdom : null;

            public string? RulerClanOf(string kingdomId) => kingdomId == "kingdom_lancaster" ? "clan_lancaster" : null;
        }
    }
}
