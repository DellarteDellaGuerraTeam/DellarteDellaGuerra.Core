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
            .AddTitle("county_lancashire", TitleRank.Earl, "duchy_lancaster", "town_lancaster", "County of Lancashire")
            .AddTitle("county_rivers", TitleRank.Earl, "duchy_lancaster", "town_chester", "County of Cheshire")
            .AddTitle("county_kent", TitleRank.Earl, "kingdom_england", "town_canterbury");

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
            new Title("county_lancashire", "Lancashire", TitleRank.Earl, "town_lancaster", "stanley"),
            new Title("county_rivers", "Cheshire", TitleRank.Earl, "town_chester", "stanley"),
            new Title("county_kent", "Kent", TitleRank.Earl, "town_canterbury", "neville"));

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
        public void ZoomingInReplacesTheEarldomWithTheUnderlyingLocalNameAndProminence()
        {
            _structure.AddTitle("county_devon", TitleRank.Baron, "kingdom_england", "exeter", "City of Exeter");
            _titles.SaveTitle(new Title("county_devon", "Earldom of Devon", TitleRank.Earl, "exeter", "stanley"));
            var painter = Painter();

            Assert.Equal("Earldom of Devon", painter.DomainLabelNameOf("county_devon", PoliticalMapOwnerLevel.Vassals));
            Assert.Equal(TitleRank.Earl, painter.DomainLabelRankOf("county_devon", PoliticalMapOwnerLevel.Vassals));
            Assert.Equal("City of Exeter", painter.DomainLabelNameOf("county_devon", PoliticalMapOwnerLevel.SubVassals));
            Assert.Equal(TitleRank.Baron, painter.DomainLabelRankOf("county_devon", PoliticalMapOwnerLevel.SubVassals));
        }

        [Fact]
        public void ClickingExeterAtCloseZoomSelectsTheCityWhileAnExistingDevonSelectionKeepsItsEstates()
        {
            _structure.AddTitle("county_devon", TitleRank.Baron, "kingdom_england", "exeter", "City of Exeter")
                .AddTitle("okehampton", TitleRank.Baron, "county_devon", "okehampton_castle", "Barony of Okehampton");
            _titles.SaveTitle(new Title("county_devon", "Earldom of Devon", TitleRank.Earl, "exeter", "somerset"));
            _titles.SaveTitle(new Title("okehampton", "Barony of Okehampton", TitleRank.Baron, "okehampton_castle", "somerset"));
            _world.Owners["exeter"] = _world.Owners["okehampton_castle"] = "clan_beaufort";
            var painter = Painter();
            string[] settlements = { "exeter", "okehampton_castle" };
            string[] territories = settlements.Select(x => painter.TerritoryOf(x, _world)).ToArray();
            var city = new PoliticalMapSelection(PoliticalMapView.Owners, PoliticalMapOwnerLevel.SubVassals,
                PoliticalMapLabelMode.Titles, "county_devon");
            var earldom = city with { Level = PoliticalMapOwnerLevel.Vassals };

            foreach (var zoom in new[] { PoliticalMapOwnerLevel.SubVassals, PoliticalMapOwnerLevel.Vassals })
            {
                var paints = painter.Paint(PoliticalMapView.Owners, settlements, territories, _folds, _world,
                    level: zoom, mode: PoliticalMapLabelMode.Titles);
                Assert.Equal("county_devon", painter.SelectionKeysOf(city, settlements, paints, _world)[0]);
                Assert.NotEqual("county_devon", painter.SelectionKeysOf(city, settlements, paints, _world)[1]);
                Assert.Equal(new[] { "county_devon", "county_devon" },
                    painter.SelectionKeysOf(earldom, settlements, paints, _world));
            }
        }

        [Fact]
        public void SelectingADuchyIncludesItsDisconnectedCountiesWithoutSelectingTheHoldersOtherTitles()
        {
            PoliticalMapPainter painter = Painter();
            var paints = Paint(PoliticalMapView.Owners, painter, level: PoliticalMapOwnerLevel.Vassals);
            var selection = new PoliticalMapSelection(PoliticalMapView.Owners, PoliticalMapOwnerLevel.Vassals,
                PoliticalMapLabelMode.Titles, "duchy_lancaster");
            string?[] keys = painter.SelectionKeysOf(selection, Settlements, paints, _world);
            var result = selection.Apply(paints, keys);

            foreach (int i in new[] { 0, 1, 2, 5 })
            {
                Assert.Equal(selection.Key, keys[i]);
                Assert.NotEqual(paints[i].Fill, result[i].Fill);
            }
            Assert.Equal(paints[3], result[3]); // Kent is outside the selected duchy.
            Assert.Equal(paints[4], result[4]); // The ruler's untitled estate is outside it too.

            var county = selection with { Key = "county_lancashire", Level = PoliticalMapOwnerLevel.SubVassals };
            var countyResult = county.Apply(paints, painter.SelectionKeysOf(county, Settlements, paints, _world));
            Assert.NotEqual(paints[0].Fill, countyResult[0].Fill);
            Assert.Equal(paints[2], countyResult[2]); // The duchy's own seat is not in this county.
            Assert.Equal(paints[5], countyResult[5]); // Same owner, different county.
        }

        [Theory]
        [InlineData(PoliticalMapOwnerLevel.Vassals)]
        [InlineData(PoliticalMapOwnerLevel.SubVassals)]
        public void SelectionUsesTheSameGeographicOrClanIdentityAsTheLabels(PoliticalMapOwnerLevel level)
        {
            PoliticalMapPainter painter = Painter();
            var paints = Paint(PoliticalMapView.Owners, painter, level: level);
            string?[] titles = painter.LabelKeysOf(PoliticalMapView.Owners, PoliticalMapLabelMode.Titles, Settlements, paints, _world, level);
            string?[] clans = painter.LabelKeysOf(PoliticalMapView.Owners, PoliticalMapLabelMode.Clans, Settlements, paints, _world, level);

            Assert.Equal(level == PoliticalMapOwnerLevel.Vassals
                ? new[] { "duchy_lancaster", "duchy_lancaster", "duchy_lancaster", "county_kent", "kingdom_england", "duchy_lancaster" }
                : new[] { "county_lancashire", "county_lancashire", "duchy_lancaster", "county_kent", "kingdom_england", "county_rivers" }, titles);
            Assert.Equal(paints.Select(paint => paint.RegionKey), clans);
            Assert.Equal(titles[0], titles[1]); // The bound village follows its seat.
            if (level == PoliticalMapOwnerLevel.Vassals)
                Assert.Equal(titles[0], titles[5]); // Constituent counties share the duchy's geographic identity.
            else
                Assert.NotEqual(titles[0], titles[5]); // Zooming in separates its counties.
            Assert.Equal(clans[0], clans[5]); // Both estates belong to the selected clan's region.

            var selected = new PoliticalMapSelection(PoliticalMapView.Owners, level, PoliticalMapLabelMode.Clans, clans[0]!);
            var highlighted = selected.Apply(paints, clans);
            Assert.NotEqual(paints[0].Fill, highlighted[0].Fill);
            Assert.NotEqual(paints[5].Fill, highlighted[5].Fill);
            Assert.Equal(paints[3], highlighted[3]);
        }

        [Theory]
        [InlineData(PoliticalMapLabelMode.Titles)]
        [InlineData(PoliticalMapLabelMode.Clans)]
        public void KingdomSelectionUsesTheFactionRegardlessOfLabelText(PoliticalMapLabelMode mode)
        {
            var paints = Paint(PoliticalMapView.Kingdom);
            Assert.Equal(paints.Select(paint => paint.RealmKey),
                Painter().LabelKeysOf(PoliticalMapView.Kingdom, mode, Settlements, paints, _world, PoliticalMapOwnerLevel.Vassals));
        }

        [Fact]
        public void ASettlementIsMeshedWithItsSeatsTerritoryOrItsSeatWhenThatHoldsNoTitle()
        {
            PoliticalMapPainter painter = Painter();

            Assert.Equal("county_lancashire", painter.TerritoryOf("village_lancaster", _world));
            Assert.Equal("duchy_lancaster", painter.TerritoryOf("castle_pontefract", _world));
            Assert.Equal("settlement:castle_lonely", painter.TerritoryOf("castle_lonely", _world));
        }

        [Fact]
        public void GeographicLabelsSeparateCountiesEvenWhenOneClanOwnsBoth()
        {
            _structure.Reattach("county_lancashire", "kingdom_england");
            _structure.Reattach("county_rivers", "kingdom_england");
            PoliticalMapPainter painter = Painter();
            Assert.Equal("clan_stanley", _world.OwnerClanOf("town_lancaster"));
            Assert.Equal("clan_stanley", _world.OwnerClanOf("town_chester"));

            Assert.Equal("county_lancashire", painter.GeographicLabelKeyOf("town_lancaster", _world, PoliticalMapOwnerLevel.Vassals));
            Assert.Equal("county_rivers", painter.GeographicLabelKeyOf("town_chester", _world, PoliticalMapOwnerLevel.Vassals));
            Assert.Equal("County of Lancashire", painter.GeographicLabelNameOf("county_lancashire"));
            Assert.Equal("County of Cheshire", painter.GeographicLabelNameOf("county_rivers"));

            // Conquest changes the owner label, never the local title's geographic identity.
            _world.Owners["town_chester"] = "clan_beaufort";
            Assert.Equal("county_rivers", painter.GeographicLabelKeyOf("town_chester", _world, PoliticalMapOwnerLevel.Vassals));
            Assert.Equal("County of Cheshire", painter.GeographicLabelNameOf("county_rivers"));
        }

        [Fact]
        public void ZoomingInRevealsTheLocalBaronyInsteadOfItsOwnersRemoteDuchy()
        {
            _structure.AddTitle("barony_lathom", TitleRank.Baron, "duchy_lancaster", "castle_lonely", "Barony of Lathom");
            // A dignity attached to the estate is not necessarily geographically local.
            _titles.SaveTitle(new Title("barony_lathom", "Duchy of Somerset", TitleRank.Duke, "castle_lonely", "somerset"));
            _world.Owners["castle_lonely"] = "clan_beaufort";
            PoliticalMapPainter painter = Painter();

            Assert.Equal("duchy_lancaster", painter.GeographicLabelKeyOf("castle_lonely", _world, PoliticalMapOwnerLevel.Vassals));
            string detailed = painter.GeographicLabelKeyOf("castle_lonely", _world, PoliticalMapOwnerLevel.SubVassals);
            Assert.Equal("barony_lathom", detailed);
            Assert.Equal("Barony of Lathom", painter.GeographicLabelNameOf(detailed));
            Assert.Equal(TitleRank.Baron, painter.GeographicLabelRankOf(detailed));
        }

        [Theory]
        [InlineData(PoliticalMapOwnerLevel.Vassals)]
        [InlineData(PoliticalMapOwnerLevel.SubVassals)]
        public void VillagesUseTheirBoundSeatsTitleAndUntitledSeatsKeepSeparatePlaceKeys(PoliticalMapOwnerLevel level)
        {
            PoliticalMapPainter painter = Painter();
            Assert.Equal(level == PoliticalMapOwnerLevel.Vassals ? "duchy_lancaster" : "county_lancashire",
                painter.GeographicLabelKeyOf("village_lancaster", _world, level));
            Assert.Equal("settlement:castle_lonely", painter.GeographicLabelKeyOf("castle_lonely", _world, level));
            Assert.Null(painter.GeographicLabelNameOf("settlement:castle_lonely"));
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
            Assert.All(paints, paint => Assert.Null(paint.ParentRegionKey));
        }

        [Fact]
        public void ZoomedInEachDirectVassalsRegionSplitsIntoTheirOwnLandAndTheirVassals()
        {
            PoliticalMapSettlementPaint[] paints = Paint(PoliticalMapView.Owners, level: PoliticalMapOwnerLevel.SubVassals);

            Assert.Equal(
                new[] { "clan_stanley", "clan_stanley", "clan_beaufort", "clan_neville", "clan_lancaster", "clan_stanley" },
                Regions(paints));
            Assert.Equal(
                new[] { "clan_beaufort", "clan_beaufort", "clan_beaufort", "clan_neville", "clan_lancaster", "clan_beaufort" },
                paints.Select(paint => paint.ParentRegionKey));
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
        public void AllOwnersRegionsOfOneKingdomShareItsFlatBaseColourAndKeepDistinctFrontierIdentities(PoliticalMapOwnerLevel level)
        {
            PoliticalMapSettlementPaint[] paints = Paint(PoliticalMapView.Owners, level: level);

            Assert.All(paints, paint => Assert.Equal(Wash(KingdomOfLancaster), paint.Fill));
            Assert.All(paints, paint => Assert.Equal(0u, paint.Hatch));
            Assert.Equal(level == PoliticalMapOwnerLevel.Vassals ? 3 : 4, paints.Select(paint => paint.FrontierKey).Distinct().Count());
            Assert.NotEqual(paints[2].FrontierKey, paints[3].FrontierKey);
        }

        [Theory]
        [InlineData("sturgia", 0xFFD41E1Eu)]
        [InlineData("vlandia", 0xFF1E64DCu)]
        [InlineData("khuzait", 0xFF19B333u)]
        [InlineData("battania", 0xFFF20DB9u)]
        public void TheOwnersViewKeepsEachKingdomsFixedColourAtBothZoomLevels(string kingdom, uint baseColour)
        {
            foreach (string clan in _world.Kingdoms.Keys.ToArray()) _world.Kingdoms[clan] = kingdom;
            PoliticalMapPainter painter = Painter();

            foreach (PoliticalMapOwnerLevel level in Enum.GetValues<PoliticalMapOwnerLevel>())
            {
                Assert.All(Paint(PoliticalMapView.Owners, painter, level: level), paint => Assert.Equal(Wash(baseColour), paint.Fill));
                Assert.Equal(baseColour, painter.MapColours(PoliticalMapView.Owners)[kingdom]);
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

        [Theory]
        [InlineData(PoliticalMapOwnerLevel.Vassals)]
        [InlineData(PoliticalMapOwnerLevel.SubVassals)]
        public void LocalRulersKeepTheirBaseColourAcrossZoomLevelsRegardlessOfWhichWasPreparedFirst(PoliticalMapOwnerLevel first)
        {
            PoliticalMapPainter painter = Painter();
            Paint(PoliticalMapView.Owners, painter, level: first);
            PoliticalMapSettlementPaint[] far = Paint(PoliticalMapView.Owners, painter, level: PoliticalMapOwnerLevel.Vassals);
            PoliticalMapSettlementPaint[] near = Paint(PoliticalMapView.Owners, painter, level: PoliticalMapOwnerLevel.SubVassals);

            // Beaufort, Neville and the king remain visible at both depths; Stanley appears close in.
            foreach (int index in new[] { 2, 3, 4 }) Assert.Equal(far[index].Fill, near[index].Fill);
            Assert.Equal(near[2].Fill, near[0].Fill);
            Assert.NotEqual(near[2].RegionKey, near[0].RegionKey);
            Assert.Equal(far, Paint(PoliticalMapView.Owners, painter, level: PoliticalMapOwnerLevel.Vassals));
        }

        [Fact]
        public void ALocalRulersBaseColourStillTracksTheirCurrentKingdom()
        {
            PoliticalMapPainter painter = Painter();
            uint oldColour = Paint(PoliticalMapView.Owners, painter, level: PoliticalMapOwnerLevel.Vassals)[2].Fill;
            _world.Kingdoms["clan_beaufort"] = "khuzait";

            PoliticalMapSettlementPaint[] near = Paint(PoliticalMapView.Owners, painter, level: PoliticalMapOwnerLevel.SubVassals);
            PoliticalMapSettlementPaint[] far = Paint(PoliticalMapView.Owners, painter, level: PoliticalMapOwnerLevel.Vassals);

            Assert.NotEqual(oldColour, near[2].Fill);
            Assert.Equal(far[2].Fill, near[2].Fill);
            Assert.Equal(Wash(PoliticalMapPalette.KingdomColor("khuzait", null)), near[2].Fill);
            Assert.Equal(Wash(KingdomOfLancaster), near[0].Fill);
        }

        [Fact]
        public void ALocalRulerWithOnlyVassalLandStillUsesTheirKingdomPalette()
        {
            _world.Owners["castle_pontefract"] = "clan_stanley";
            PoliticalMapPainter painter = Painter();
            Paint(PoliticalMapView.Owners, painter, level: PoliticalMapOwnerLevel.SubVassals);

            PoliticalMapSettlementPaint[] far = Paint(PoliticalMapView.Owners, painter, level: PoliticalMapOwnerLevel.Vassals);

            Assert.Equal("clan_beaufort", far[2].RegionKey);
            Assert.Equal(Wash(KingdomOfLancaster), far[2].Fill);
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

            float nearDistance = PoliticalMapZoom.FarAbove - PoliticalMapZoom.OwnerLevelHysteresis - 1f;
            float farDistance = PoliticalMapZoom.FarAbove + 1f;
            float bandDistance = PoliticalMapZoom.FarAbove - PoliticalMapZoom.OwnerLevelHysteresis / 2f;
            string?[] near = PaintAt(nearDistance);
            string?[] far = PaintAt(farDistance);
            Assert.Equal("clan_stanley", near[0]);
            Assert.Equal("clan_beaufort", far[0]);
            Assert.Equal("clan_lancaster", far[4]);
            Assert.Equal(far, PaintAt(bandDistance));
            Assert.Equal(near, PaintAt(nearDistance));
            Assert.Equal(near, PaintAt(bandDistance));
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
