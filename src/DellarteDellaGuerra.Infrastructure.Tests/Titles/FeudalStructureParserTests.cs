using DellarteDellaGuerra.Domain.Titles.Model;
using DellarteDellaGuerra.Infrastructure.Titles;
using DellarteDellaGuerra.Infrastructure.Titles.Model;

namespace DellarteDellaGuerra.Infrastructure.Tests.Titles;

public class FeudalStructureParserTests
{
    [Fact]
    public void GeographicLabelRankDoesNotChangeStructuralTierOrPersonalDignity()
    {
        var forest = FeudalStructureParser.Parse(
            "<Feudalism><Kingdom id='k'><Duchy id='d' mapLabelRank='Baron' dignityRank='Duke'/></Kingdom></Feudalism>");
        var title = Assert.Single(Assert.Single(forest).Children);
        Assert.Equal(TitleRank.Duke, title.Rank);
        Assert.Equal(TitleRank.Duke, title.DignityRank);
        Assert.Equal(TitleRank.Baron, title.MapLabelRank);
        Assert.Equal(TitleRank.Baron, new XmlFeudalStructure(forest).GetMapLabelRank("d"));
    }

    [Theory]
    [InlineData("1")]
    [InlineData("Unknown")]
    [InlineData("")]
    public void RejectsInvalidMapLabelRanks(string rank) => Assert.Throws<InvalidOperationException>(() =>
        FeudalStructureParser.Parse($"<Feudalism><Kingdom id='k' mapLabelRank='{rank}'/></Feudalism>"));

    [Theory]
    [InlineData("Unknown")]
    [InlineData("King")]
    [InlineData("1")]
    [InlineData("")]
    public void RejectsInvalidDignityRanks(string rank)
    {
        Assert.Throws<InvalidOperationException>(() => FeudalStructureParser.Parse(
            $"<Feudalism><Kingdom id='k'><County id='c' dignityRank='{rank}'/></Kingdom></Feudalism>"));
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("c")]
    [InlineData("b")]
    [InlineData("other")]
    [InlineData("")]
    public void RejectsUnknownCrossRealmEmptyAndCyclicAllegiances(string target)
    {
        Assert.Throws<InvalidOperationException>(() => FeudalStructureParser.Parse(
            $"<Feudalism><Kingdom id='k'><County id='c' allegiance='{target}'><Barony id='b'/></County></Kingdom><Kingdom id='other'/></Feudalism>"));
    }

    [Fact]
    public void RejectsCyclesMadeEntirelyOfExplicitAllegiances()
    {
        Assert.Throws<InvalidOperationException>(() => FeudalStructureParser.Parse(
            "<Feudalism><Kingdom id='k'><County id='a' allegiance='b'/><County id='b' allegiance='a'/></Kingdom></Feudalism>"));
    }

    private const string SampleXml =
        """
        <Feudalism>
          <Kingdom id="kingdom_england" name="England" seat="town_london" kingClanId="clan_lancaster">
            <Duchy id="duchy_york" name="Duchy of York" seat="town_york" holderClanId="clan_york" heldSince="1385">
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
        Assert.Equal(TitleRank.Earl, kingdom.Children[0].Children[0].Rank);
        Assert.Equal(TitleRank.Baron, kingdom.Children[0].Children[0].Children[0].Rank);
        Assert.Equal(TitleRank.Earl, kingdom.Children[1].Rank);
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
        Assert.Equal(1385, duchy.InitialHeldSinceYear);
        Assert.Null(kingdom.InitialHeldSinceYear);
    }

    [Fact]
    public void ShouldThrowOnInvalidHeldSinceYear()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => FeudalStructureParser.Parse(
            """
            <Feudalism>
              <Kingdom id="kingdom_england" kingClanId="clan_lancaster" heldSince="c. 1399"/>
            </Feudalism>
            """));

        Assert.Contains("heldSince", exception.Message);
        Assert.Contains("kingdom_england", exception.Message);
    }

    [Fact]
    public void ShouldParseClaimsOnTheirParentTitleRatherThanAsVassals()
    {
        var forest = FeudalStructureParser.Parse(
            """
            <Feudalism>
              <Kingdom id="kingdom_england" kingClanId="clan_lancaster">
                <Claim clanId="clan_york"/>
                <Barony id="barony_skipton" seat="castle_skipton" holderClanId="clan_stanley">
                  <Claim clanId="clan_clifford" strength="Weak"/>
                </Barony>
              </Kingdom>
            </Feudalism>
            """);

        var kingdom = Assert.Single(forest);
        var barony = Assert.Single(kingdom.Children);
        Assert.Equal(new FeudalClaimNode("clan_york", ClaimStrength.Strong), Assert.Single(kingdom.InitialClaims!));
        Assert.Equal(new FeudalClaimNode("clan_clifford", ClaimStrength.Weak), Assert.Single(barony.InitialClaims!));
        Assert.Empty(barony.Children);
    }

    [Theory]
    [InlineData("<Claim clanId=\"clan_york\" strength=\"Huge\"/>", "Huge")]
    [InlineData("<Claim clanId=\"clan_york\" strength=\"7\"/>", "7")]
    [InlineData("<Claim clanId=\"clan_lancaster\"/>", "already holds")]
    [InlineData("<Claim strength=\"Weak\"/>", "clanId")]
    public void ShouldThrowOnAnInvalidClaim(string claim, string expectedInMessage)
    {
        var exception = Assert.Throws<InvalidOperationException>(() => FeudalStructureParser.Parse(
            $"""
            <Feudalism>
              <Kingdom id="kingdom_england" kingClanId="clan_lancaster">{claim}</Kingdom>
            </Feudalism>
            """));

        Assert.Contains(expectedInMessage, exception.Message);
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
