using DellarteDellaGuerra.Domain.PoliticalMap;
using DellarteDellaGuerra.Domain.PoliticalMap.Labels;
using DellarteDellaGuerra.Domain.PoliticalMap.Overlay;
using DellarteDellaGuerra.Domain.Tests.Titles;
using DellarteDellaGuerra.Domain.Titles.Model;

namespace DellarteDellaGuerra.Domain.Tests.PoliticalMap.Overlay;

public class PoliticalMapDeJureTests
{
    private static readonly string[] Settlements = { "a", "village", "b", "other", "unmapped" };
    private readonly FakeFeudalStructure _structure = new FakeFeudalStructure()
        .AddTitle("realm", TitleRank.King)
        .AddTitle("duchy", TitleRank.Duke, "realm", name: "Duchy")
        .AddTitle("county_a", TitleRank.Earl, "duchy", "a", "County A")
        .AddTitle("county_b", TitleRank.Earl, "duchy", "b", "County B")
        .AddTitle("outside", TitleRank.Earl, "realm", "other", "Outside");
    private readonly World _world = new();

    private PoliticalMapPainter Painter()
    {
        var titles = new FakeTitleRepository();
        return new(_structure, titles, new FakeGenealogy(), new PoliticalMapTerritories(_structure, titles));
    }

    private PoliticalMapSettlementPaint[] Paint(PoliticalMapPainter painter, PoliticalMapOwnerLevel level) =>
        painter.Paint(PoliticalMapView.DeJure, Settlements, Settlements.Select(x => painter.TerritoryOf(x, _world)).ToArray(),
            new Dictionary<string, string>(), _world, level: level, mode: PoliticalMapLabelMode.Titles);

    [Theory]
    [InlineData(PoliticalMapOwnerLevel.Vassals)]
    [InlineData(PoliticalMapOwnerLevel.SubVassals)]
    public void BordersLabelsAndBoundVillagesUseGeographicIdentityWithoutReadingOwnership(PoliticalMapOwnerLevel level)
    {
        var painter = Painter();
        var paints = Paint(painter, level);
        string?[] labels = painter.LabelKeysOf(PoliticalMapView.DeJure, PoliticalMapLabelMode.Titles,
            Settlements, paints, _world, level);
        Assert.Equal(Settlements.Select(x => painter.GeographicLabelKeyOf(x, _world, level)), labels);
        Assert.Equal(labels, paints.Select(x => x.RegionKey));
        Assert.Equal(paints[0], paints[1]);
        Assert.All(paints, paint => Assert.NotEqual(0u, paint.Fill));
        Assert.Equal(level == PoliticalMapOwnerLevel.Vassals ? "duchy" : "county_a", labels[0]);
        Assert.Equal("settlement:unmapped", labels[4]);
        Assert.Equal(level == PoliticalMapOwnerLevel.SubVassals ? "duchy" : null, paints[0].ParentRegionKey);
        Assert.Empty(painter.MapColours(PoliticalMapView.DeJure));
    }

    [Theory]
    [InlineData(PoliticalMapOwnerLevel.Vassals, "Duchy")]
    [InlineData(PoliticalMapOwnerLevel.SubVassals, "County A")]
    public void HoverNamesTheGeographicTitleAtTheDisplayedLevel(PoliticalMapOwnerLevel level, string expected)
    {
        var hover = new PoliticalMapHoverDescriber(Painter()).Describe(PoliticalMapView.DeJure, "village", _world, _world, level);
        Assert.Equal(expected, hover!.TitleName);
        Assert.Null(hover.OwnerClanName);
        Assert.Null(hover.FactionName);
        Assert.Null(hover.LiegeClanName);
    }

    [Fact]
    public void ZoomingOutPromotesTheClickedCountyToItsGeographicDuchyWithoutReadingOwnership()
    {
        var selection = new PoliticalMapSelection(PoliticalMapView.DeJure, PoliticalMapOwnerLevel.SubVassals,
            PoliticalMapLabelMode.Titles, "county_a");
        Assert.Equal(selection with { Key = "duchy", Level = PoliticalMapOwnerLevel.Vassals },
            Painter().PromoteSelection(selection, PoliticalMapOwnerLevel.Vassals, "a", _world));
    }

    [Fact]
    public void SelectingADuchyIncludesItsWholeGeographicExtentAcrossZoomLevels()
    {
        var painter = Painter();
        var paints = Paint(painter, PoliticalMapOwnerLevel.SubVassals);
        var selection = new PoliticalMapSelection(PoliticalMapView.DeJure, PoliticalMapOwnerLevel.Vassals,
            PoliticalMapLabelMode.Titles, "duchy");
        var keys = painter.SelectionKeysOf(selection, Settlements, paints, _world);
        var selected = selection.Apply(paints, keys);
        foreach (int i in new[] { 0, 1, 2 })
        {
            Assert.Equal("duchy", keys[i]);
            Assert.NotEqual(paints[i].Fill, selected[i].Fill);
        }
        Assert.Equal(paints[3], selected[3]);
        Assert.Equal(paints[4], selected[4]);
    }

    [Theory]
    [InlineData(PoliticalMapOwnerLevel.Vassals, "region")]
    [InlineData(PoliticalMapOwnerLevel.SubVassals, "shire")]
    public void ADeepHierarchyShowsTheRegionFarOutAndTheShireCloseIn(PoliticalMapOwnerLevel level, string expected)
    {
        _structure
            .AddTitle("region", TitleRank.Duke, "realm")
            .AddTitle("shire", TitleRank.Duke, "region")
            .AddTitle("county_deep", TitleRank.Earl, "shire", "deep")
            .AddTitle("barony_deep", TitleRank.Baron, "county_deep", "deeper");

        Assert.Equal(expected, Painter().GeographicLabelKeyOf("deep", _world, level));
        Assert.Equal(expected, Painter().GeographicLabelKeyOf("deeper", _world, level));
    }

    // Throwing accessors ensure de jure rendering remains independent of live control and allegiance.
    private sealed class World : IPoliticalMapWorld, IPoliticalMapNames
    {
        public string? SeatOf(string id) => id == "village" ? "a" : id;
        public string? OwnerClanOf(string id) => throw new InvalidOperationException("Owner read in de jure view");
        public string? FactionOf(string id) => throw new InvalidOperationException("Faction read in de jure view");
        public uint? ClanColor(string id) => throw new InvalidOperationException("Clan read in de jure view");
        public uint? FactionColor(string id) => throw new InvalidOperationException("Faction read in de jure view");
        public string? KingdomOfClan(string id) => throw new InvalidOperationException("Allegiance read in de jure view");
        public string? RulerClanOf(string id) => throw new InvalidOperationException("Ruler read in de jure view");
        public string? SettlementName(string id) => id;
        public string? FactionName(string id) => throw new InvalidOperationException("Faction name read in de jure view");
        public string? ClanName(string id) => throw new InvalidOperationException("Clan name read in de jure view");
    }
}
