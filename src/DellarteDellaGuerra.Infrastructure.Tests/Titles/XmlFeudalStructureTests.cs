using DellarteDellaGuerra.Domain.Titles.Model;
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
    public void ShouldBuildInitialTitles()
    {
        IReadOnlyList<Title> titles = _feudalStructure.BuildInitialTitles();

        Assert.Equal(5, titles.Count);

        Title kingdom = titles.Single(title => title.Id == "kingdom_england");
        Assert.Equal("England", kingdom.Name);
        Assert.Equal(TitleRank.King, kingdom.Rank);
        Assert.Equal(string.Empty, kingdom.SeatSettlementId);
        Assert.Equal("clan_lancaster", kingdom.HolderClanId);

        Title barony = titles.Single(title => title.Id == "barony_middleham");
        Assert.Equal(TitleRank.Baron, barony.Rank);
        Assert.Equal("castle_middleham", barony.SeatSettlementId);
        Assert.Null(barony.HolderClanId);
    }
}
