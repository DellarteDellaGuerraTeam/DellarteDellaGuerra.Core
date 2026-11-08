using DellarteDellaGuerra.Domain.PoliticalMap;
using DellarteDellaGuerra.Domain.PoliticalMap.Labels;
using DellarteDellaGuerra.Domain.PoliticalMap.Overlay;
using DellarteDellaGuerra.Domain.Tests.Titles;
using DellarteDellaGuerra.Domain.Titles.Model;

namespace DellarteDellaGuerra.Domain.Tests.PoliticalMap.Overlay;

public class PoliticalMapTitleDomainsTests
{
    private readonly FakeFeudalStructure _structure = new();
    private readonly FakeTitleRepository _titles = new();
    private readonly FakeGenealogy _genealogy = new();
    private readonly World _world = new();

    public PoliticalMapTitleDomainsTests()
    {
        Add("crown", TitleRank.King, null, "king");
        Add("duchy", TitleRank.Duke, "crown", "lord");
        Add("pembroke", TitleRank.Earl, "crown", "lord");
        Add("pembroke_castle", TitleRank.Baron, "pembroke", "lord");
        Add("duchy_castle", TitleRank.Baron, "duchy", "lord");
        Add("pembroke_vassal", TitleRank.Baron, "pembroke", "tenant");
        Add("distant_estate", TitleRank.Baron, "crown", "tenant");
        Add("duchy_vassal", TitleRank.Earl, "duchy", "other_tenant");
        Add("subvassal", TitleRank.Baron, "duchy_vassal", "subtenant");
        Add("outsider", TitleRank.Earl, "crown", "outsider");
    }

    private void Add(string id, TitleRank rank, string? parent, string clan)
    {
        _structure.AddTitle(id, rank, parent, id + "_seat", id);
        _genealogy.AddHero(clan, clan);
        _titles.SaveTitle(new Title(id, id, rank, id + "_seat", clan,
            heldSinceDay: id == "distant_estate" ? 100 : 0));
        _world.Owners[id + "_seat"] = clan;
    }

    private PoliticalMapPainter Painter() => new(_structure, _titles, _genealogy, new PoliticalMapTerritories(_structure, _titles));
    private PoliticalMapTitleDomains Domains() => Painter().TitleDomainsOf(_world);

    [Fact]
    public void MainEarldomGroupsDisconnectedPersonalBaroniesButNotAnotherEarldom()
    {
        Add("devon", TitleRank.Earl, "crown", "earl");
        Add("okehampton", TitleRank.Baron, "crown", "earl");
        Add("mount", TitleRank.Baron, "crown", "earl");
        Add("secondary_earldom", TitleRank.Earl, "crown", "earl");
        foreach (string estate in new[] { "okehampton", "mount" })
        {
            Assert.Equal("devon", Domains().KeyOf(estate + "_seat", PoliticalMapOwnerLevel.Vassals));
            Assert.Equal(estate, Domains().KeyOf(estate + "_seat", PoliticalMapOwnerLevel.SubVassals));
            Assert.True(Domains().Contains("devon", estate + "_seat"));
            Assert.True(Domains().Contains(estate, estate + "_seat"));
        }
        Assert.Equal("secondary_earldom", Domains().KeyOf("secondary_earldom_seat", PoliticalMapOwnerLevel.Vassals));
        Assert.False(Domains().Contains("devon", "secondary_earldom_seat"));
    }

    [Fact]
    public void SecondaryEarldomIncludesItsOwnEstatesAndItsVassalsDistantLandOnly()
    {
        var domains = Domains();
        Assert.True(domains.Contains("pembroke", "pembroke_seat"));
        Assert.True(domains.Contains("pembroke", "pembroke_castle_seat"));
        Assert.True(domains.Contains("pembroke", "pembroke_vassal_seat"));
        Assert.True(domains.Contains("pembroke", "distant_estate_seat"));
        Assert.False(domains.Contains("pembroke", "duchy_seat"));
        Assert.False(domains.Contains("pembroke", "duchy_vassal_seat"));
        Assert.False(domains.Contains("pembroke", "outsider_seat"));
        Assert.Equal("pembroke", domains.KeyOf("distant_estate_seat", PoliticalMapOwnerLevel.Vassals));
        Assert.Equal("distant_estate", domains.KeyOf("distant_estate_seat", PoliticalMapOwnerLevel.SubVassals));
    }

    [Fact]
    public void PrimaryTitleGathersUnattachedVassalsAndUntitledPersonalLand()
    {
        // Unattached clans answer to the Crown, not to a secondary duchy held by the king.
        _titles.SaveTitle(_titles.GetTitle("crown")!.GrantTo("lord", 1));
        _world.Ruler = "lord";
        Add("unattached", TitleRank.Baron, "crown", "unattached");
        _world.Owners["untitled"] = "lord";
        var domains = Domains();
        Assert.True(domains.Contains("crown", "unattached_seat"));
        Assert.True(domains.Contains("crown", "untitled"));
        Assert.False(domains.Contains("duchy", "unattached_seat"));
        Assert.False(domains.Contains("duchy", "untitled"));
        Assert.False(domains.Contains("pembroke", "unattached_seat"));
        Assert.False(domains.Contains("duchy", "pembroke_seat"));
        // A direct Crown vassal retains its own label; selecting the ruler's primary domain includes it.
        Assert.Equal("unattached", domains.KeyOf("unattached_seat", PoliticalMapOwnerLevel.Vassals));
    }

    [Fact]
    public void ForeignOrUnswornOccupiersDoNotJoinADeJureTitleDomain()
    {
        _world.Owners["pembroke_castle_seat"] = "outsider";
        Assert.False(Domains().Contains("pembroke", "pembroke_castle_seat"));
        _world.Foreign.Add("tenant");
        Assert.False(Domains().Contains("pembroke", "pembroke_vassal_seat"));
    }

    [Fact]
    public void ChangingLandedTitleDoesNotReassignCrownVassals()
    {
        _titles.SaveTitle(_titles.GetTitle("crown")!.GrantTo("lord", 1));
        _world.Ruler = "lord";
        Add("unattached", TitleRank.Baron, "crown", "unattached");
        Assert.True(Domains().Contains("crown", "unattached_seat"));
        Assert.False(Domains().Contains("duchy", "unattached_seat"));
        _titles.SaveTitle(_titles.GetTitle("duchy")!.GrantTo("outsider", 2));
        _world.Owners["duchy_seat"] = "outsider";
        Assert.True(Domains().Contains("crown", "unattached_seat"));
        Assert.False(Domains().Contains("pembroke", "unattached_seat"));
        Assert.True(Domains().Contains("pembroke", "distant_estate_seat"));
        Assert.False(Domains().Contains("duchy", "unattached_seat"));
    }

    [Fact]
    public void LancashireAllegianceJoinsLancasterDespiteSiblingGeographyWithoutAbsorbingTheRealm()
    {
        _titles.SaveTitle(_titles.GetTitle("crown")!.GrantTo("lord", 1));
        _world.Ruler = "lord";
        Add("lancashire", TitleRank.Earl, "crown", "lord");
        _structure.WithAllegiance("lancashire", "duchy");
        var domains = Domains();
        Assert.Equal("duchy", domains.KeyOf("lancashire_seat", PoliticalMapOwnerLevel.Vassals));
        Assert.True(domains.Contains("duchy", "lancashire_seat"));
        Assert.False(domains.Contains("duchy", "outsider_seat"));
        Assert.True(domains.Contains("crown", "outsider_seat"));
        Assert.Equal("lancashire", domains.KeyOf("lancashire_seat", PoliticalMapOwnerLevel.SubVassals));
    }

    [Fact]
    public void AllegianceChoosesTheSecondaryDomainEvenWhenGeographyDiffers()
    {
        _structure.Reattach("pembroke_vassal", "duchy");
        _structure.WithAllegiance("pembroke_vassal", "pembroke");
        Assert.True(Domains().Contains("pembroke", "pembroke_vassal_seat"));
        Assert.False(Domains().Contains("duchy", "pembroke_vassal_seat"));
    }

    [Theory]
    [InlineData(PoliticalMapOwnerLevel.Vassals)]
    [InlineData(PoliticalMapOwnerLevel.SubVassals)]
    public void PaintLabelsSelectionAndBoundVillagesUseTheSameTitleDomains(PoliticalMapOwnerLevel level)
    {
        _world.Owners["pembroke_castle_seat"] = "outsider";
        string[] settlements = { "pembroke_seat", "village", "pembroke_vassal_seat", "distant_estate_seat", "duchy_seat", "pembroke_castle_seat" };
        var painter = Painter();
        var territories = settlements.Select(x => painter.TerritoryOf(x, _world)).ToArray();
        var paints = painter.Paint(PoliticalMapView.Owners, settlements, territories,
            new Dictionary<string, string>(), _world, level: level, mode: PoliticalMapLabelMode.Titles);
        var labels = painter.LabelKeysOf(PoliticalMapView.Owners, PoliticalMapLabelMode.Titles, settlements, paints, _world, level);
        Assert.Equal(labels, paints.Select(x => x.RegionKey));
        Assert.Equal(labels[0], labels[1]);
        Assert.NotEqual(labels[0], labels[4]);
        var selection = new PoliticalMapSelection(PoliticalMapView.Owners, level, PoliticalMapLabelMode.Titles, "pembroke");
        var selected = selection.Apply(paints, painter.SelectionKeysOf(selection, settlements, paints, _world));
        for (int i = 0; i < 4; i++) Assert.NotEqual(paints[i].Fill, selected[i].Fill);
        for (int i = 4; i < 6; i++) Assert.Equal(paints[i], selected[i]);
    }

    [Fact]
    public void ClanSelectionIncludesAllTitlesAndEveryDepthOfVassals()
    {
        string[] settlements = { "duchy_seat", "pembroke_seat", "pembroke_vassal_seat", "distant_estate_seat", "subvassal_seat", "outsider_seat" };
        var painter = Painter();
        var paints = painter.Paint(PoliticalMapView.Owners, settlements,
            settlements.Select(x => painter.TerritoryOf(x, _world)).ToArray(), new Dictionary<string, string>(), _world);
        var selection = new PoliticalMapSelection(PoliticalMapView.Owners, PoliticalMapOwnerLevel.SubVassals, PoliticalMapLabelMode.Clans, "lord");
        var selected = selection.Apply(paints, painter.SelectionKeysOf(selection, settlements, paints, _world));
        for (int i = 0; i < 5; i++) Assert.NotEqual(paints[i].Fill, selected[i].Fill);
        Assert.Equal(paints[5], selected[5]);
    }

    private sealed class World : IPoliticalMapWorld
    {
        public readonly Dictionary<string, string> Owners = new();
        public readonly HashSet<string> Foreign = new();
        public string Ruler = "king";
        public string? SeatOf(string id) => id == "village" ? "pembroke_seat" : id;
        public string? OwnerClanOf(string id) => Owners.GetValueOrDefault(SeatOf(id)!);
        public string? KingdomOfClan(string id) => Foreign.Contains(id) ? "foreign" : "realm";
        public string? RulerClanOf(string id) => id == "realm" ? Ruler : "foreign_king";
        public string? FactionOf(string id) => OwnerClanOf(id) is { } owner ? KingdomOfClan(owner) : null;
        public uint? ClanColor(string id) => 0xFFAB1234;
        public uint? FactionColor(string id) => 0xFFAB1234;
    }
}
