using DellarteDellaGuerra.Domain.Titles.Model;
using DellarteDellaGuerra.Domain.Titles.Port;
using DellarteDellaGuerra.Infrastructure.Titles;

namespace DellarteDellaGuerra.Infrastructure.Tests.Titles;

public class XmlFeudalStructureTests
{
    private const string SampleXml =
        """
        <Feudalism>
          <Kingdom id="kingdom_england" name="England" kingClanId="clan_lancaster">
            <Duchy id="duchy_york" name="Duchy of York" seat="town_york" holderClanId="clan_york">
              <County id="county_richmond" name="County of Richmond" seat="town_richmond" holderClanId="clan_neville">
                <Barony id="barony_middleham" name="Barony of Middleham" seat="castle_middleham"/>
              </County>
            </Duchy>
            <County id="county_kent" name="County of Kent" seat="town_kent"/>
          </Kingdom>
        </Feudalism>
        """;

    private readonly XmlFeudalStructure _feudalStructure =
        new XmlFeudalStructure(FeudalStructureParser.Parse(SampleXml));

    private readonly IdentityGenealogy _genealogy = new();

    [Fact]
    public void ShouldResolveDeJureSuzerain()
    {
        Assert.Null(_feudalStructure.GetDeJureSuzerainTitleId("kingdom_england"));
        Assert.Equal("kingdom_england", _feudalStructure.GetDeJureSuzerainTitleId("duchy_york"));
        Assert.Equal("kingdom_england", _feudalStructure.GetDeJureSuzerainTitleId("county_kent"));
        Assert.Equal("duchy_york", _feudalStructure.GetDeJureSuzerainTitleId("county_richmond"));
        Assert.Equal("county_richmond", _feudalStructure.GetDeJureSuzerainTitleId("barony_middleham"));
    }

    [Fact]
    public void ShouldResolveDeJureVassals()
    {
        Assert.Equal(new[] { "duchy_york", "county_kent" },
            _feudalStructure.GetDeJureVassalTitleIds("kingdom_england"));
        Assert.Equal(new[] { "county_richmond" }, _feudalStructure.GetDeJureVassalTitleIds("duchy_york"));
        Assert.Empty(_feudalStructure.GetDeJureVassalTitleIds("barony_middleham"));
        Assert.Empty(_feudalStructure.GetDeJureVassalTitleIds("unknown_title"));
    }

    [Fact]
    public void ShouldResolveTitleBySeat()
    {
        Assert.Equal("duchy_york", _feudalStructure.GetTitleIdBySeat("town_york"));
        Assert.Equal("barony_middleham", _feudalStructure.GetTitleIdBySeat("castle_middleham"));
        Assert.Null(_feudalStructure.GetTitleIdBySeat("town_unknown"));
    }

    [Fact]
    public void ShouldExposeAllTitleIdsRanksAndNames()
    {
        Assert.Equal(
            new[] { "kingdom_england", "duchy_york", "county_richmond", "barony_middleham", "county_kent" },
            _feudalStructure.GetAllTitleIds());
        Assert.Equal(TitleRank.King, _feudalStructure.GetRank("kingdom_england"));
        Assert.Equal(TitleRank.Baron, _feudalStructure.GetRank("barony_middleham"));
        Assert.Null(_feudalStructure.GetRank("unknown_title"));
        Assert.Equal("Duchy of York", _feudalStructure.GetTitleName("duchy_york"));
        Assert.Null(_feudalStructure.GetTitleName("unknown_title"));
    }

    [Fact]
    public void ShouldReattachATitleWithEverythingBelowIt()
    {
        _feudalStructure.Reattach("county_richmond", "kingdom_england");

        Assert.Equal("kingdom_england", _feudalStructure.GetDeJureSuzerainTitleId("county_richmond"));
        Assert.Equal(new[] { "duchy_york", "county_kent", "county_richmond" },
            _feudalStructure.GetDeJureVassalTitleIds("kingdom_england"));
        Assert.Empty(_feudalStructure.GetDeJureVassalTitleIds("duchy_york"));
        Assert.Equal("county_richmond", _feudalStructure.GetDeJureSuzerainTitleId("barony_middleham"));
    }

    [Fact]
    public void ShouldReattachATitleAsARoot()
    {
        _feudalStructure.Reattach("duchy_york", null);

        Assert.Null(_feudalStructure.GetDeJureSuzerainTitleId("duchy_york"));
        Assert.Equal(new[] { "county_kent" }, _feudalStructure.GetDeJureVassalTitleIds("kingdom_england"));
    }

    [Fact]
    public void ShouldRestoreTheConfiguredHierarchyWithTheSavedReattachments()
    {
        _feudalStructure.Reattach("county_richmond", "kingdom_england");
        IReadOnlyDictionary<string, string?> saved = _feudalStructure.SnapshotReattachments();

        _feudalStructure.InitialiseReattachments(new Dictionary<string, string?>());
        Assert.Equal("duchy_york", _feudalStructure.GetDeJureSuzerainTitleId("county_richmond"));
        Assert.Equal(new[] { "duchy_york", "county_kent" },
            _feudalStructure.GetDeJureVassalTitleIds("kingdom_england"));
        Assert.Empty(_feudalStructure.SnapshotReattachments());

        _feudalStructure.InitialiseReattachments(saved);
        Assert.Equal("kingdom_england", _feudalStructure.GetDeJureSuzerainTitleId("county_richmond"));
        Assert.Empty(_feudalStructure.GetDeJureVassalTitleIds("duchy_york"));
    }

    [Fact]
    public void ShouldSkipSavedReattachmentsOfUnknownTitles()
    {
        _feudalStructure.InitialiseReattachments(new Dictionary<string, string?>
        {
            ["removed_title"] = "kingdom_england",
            ["county_kent"] = "removed_title"
        });

        Assert.Equal("kingdom_england", _feudalStructure.GetDeJureSuzerainTitleId("county_kent"));
        Assert.Equal(5, _feudalStructure.GetAllTitleIds().Count);
    }

    [Fact]
    public void ShouldBuildInitialTitles()
    {
        IReadOnlyList<Title> titles = _feudalStructure.BuildInitialTitles(_genealogy);

        Assert.Equal(5, titles.Count);

        Title kingdom = titles.Single(title => title.Id == "kingdom_england");
        Assert.Equal("England", kingdom.Name);
        Assert.Equal(TitleRank.King, kingdom.Rank);
        Assert.Equal(string.Empty, kingdom.SeatSettlementId);
        Assert.Equal("clan_lancaster", _genealogy.GetHolderClanOf(kingdom));

        Title barony = titles.Single(title => title.Id == "barony_middleham");
        Assert.Equal(TitleRank.Baron, barony.Rank);
        Assert.Equal("castle_middleham", barony.SeatSettlementId);
        Assert.Null(_genealogy.GetHolderClanOf(barony));
    }
}
