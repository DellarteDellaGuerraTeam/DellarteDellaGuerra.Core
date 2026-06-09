using DellarteDellaGuerra.Domain.Titles.Model;
using DellarteDellaGuerra.Infrastructure.Titles;
using DellarteDellaGuerra.Infrastructure.Titles.Model;

namespace DellarteDellaGuerra.Infrastructure.Tests.Titles;

public class FeudalStructureParserTests
{
    private const string SampleXml =
        """
        <Feudalism>
          <Kingdom id="kingdom_england" name="England" seat="town_london" kingClanId="clan_lancaster">
            <Duchy id="duchy_york" name="Duchy of York" seat="town_york" holderClanId="clan_york">
              <County id="county_richmond" name="County of Richmond" seat="town_richmond" holderClanId="clan_neville">
                <Barony id="barony_middleham" name="Barony of Middleham" seat="castle_middleham" holderClanId="clan_neville_cadet"/>
              </County>
            </Duchy>
            <County id="county_kent" name="County of Kent" seat="town_kent"/>
          </Kingdom>
        </Feudalism>
        """;

    [Fact]
    public void ShouldParseRanksFromElementNames()
    {
        IReadOnlyList<FeudalTitleNode> forest = FeudalStructureParser.Parse(SampleXml);

        FeudalTitleNode kingdom = Assert.Single(forest);
        Assert.Equal(TitleRank.King, kingdom.Rank);
        Assert.Equal(TitleRank.Duke, kingdom.Children[0].Rank);
        Assert.Equal(TitleRank.Count, kingdom.Children[0].Children[0].Rank);
        Assert.Equal(TitleRank.Baron, kingdom.Children[0].Children[0].Children[0].Rank);
        Assert.Equal(TitleRank.Count, kingdom.Children[1].Rank);
    }

    [Fact]
    public void ShouldParseAttributes()
    {
        IReadOnlyList<FeudalTitleNode> forest = FeudalStructureParser.Parse(SampleXml);

        FeudalTitleNode kingdom = forest[0];
        Assert.Equal("kingdom_england", kingdom.TitleId);
        Assert.Equal("England", kingdom.Name);
        Assert.Equal("town_london", kingdom.SeatSettlementId);
        Assert.Equal("clan_lancaster", kingdom.InitialHolderClanId);

        FeudalTitleNode duchy = kingdom.Children[0];
        Assert.Equal("duchy_york", duchy.TitleId);
        Assert.Equal("clan_york", duchy.InitialHolderClanId);
    }

    [Fact]
    public void ShouldBuildParentChildHierarchy()
    {
        IReadOnlyList<FeudalTitleNode> forest = FeudalStructureParser.Parse(SampleXml);

        FeudalTitleNode kingdom = forest[0];
        Assert.Equal(2, kingdom.Children.Count);
        Assert.Equal("duchy_york", kingdom.Children[0].TitleId);
        Assert.Equal("county_kent", kingdom.Children[1].TitleId);
        Assert.Equal("county_richmond", kingdom.Children[0].Children[0].TitleId);
        Assert.Equal("barony_middleham", kingdom.Children[0].Children[0].Children[0].TitleId);
        Assert.Empty(kingdom.Children[1].Children);
    }

    [Fact]
    public void ShouldTolerateMissingOptionalAttributes()
    {
        IReadOnlyList<FeudalTitleNode> forest = FeudalStructureParser.Parse(
            """
            <Feudalism>
              <Kingdom id="kingdom_no_seat"/>
            </Feudalism>
            """);

        FeudalTitleNode kingdom = Assert.Single(forest);
        Assert.Equal(string.Empty, kingdom.SeatSettlementId);
        Assert.Null(kingdom.InitialHolderClanId);
        Assert.Equal("kingdom_no_seat", kingdom.Name);
    }

    [Fact]
    public void ShouldThrowOnMissingId()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => FeudalStructureParser.Parse(
            """
            <Feudalism>
              <Kingdom name="England"/>
            </Feudalism>
            """));

        Assert.Contains("id", exception.Message);
        Assert.Contains("Kingdom", exception.Message);
    }

    [Fact]
    public void ShouldThrowOnDuplicateTitleId()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => FeudalStructureParser.Parse(
            """
            <Feudalism>
              <Kingdom id="kingdom_england" name="England">
                <Duchy id="duchy_york" seat="town_york"/>
                <Duchy id="duchy_york" seat="town_other"/>
              </Kingdom>
            </Feudalism>
            """));

        Assert.Contains("duchy_york", exception.Message);
    }

    [Fact]
    public void ShouldThrowOnDuplicateSeat()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => FeudalStructureParser.Parse(
            """
            <Feudalism>
              <Kingdom id="kingdom_england" name="England">
                <Duchy id="duchy_york" seat="town_york"/>
                <Duchy id="duchy_lancaster" seat="town_york"/>
              </Kingdom>
            </Feudalism>
            """));

        Assert.Contains("town_york", exception.Message);
    }

    [Fact]
    public void ShouldThrowOnUnknownTitleElement()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => FeudalStructureParser.Parse(
            """
            <Feudalism>
              <Margraviate id="margraviate_unknown"/>
            </Feudalism>
            """));

        Assert.Contains("Margraviate", exception.Message);
    }
}
