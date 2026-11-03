using DellarteDellaGuerra.Domain.PoliticalMap;
using DellarteDellaGuerra.Domain.Tests.Titles;
using DellarteDellaGuerra.Domain.Titles.Model;

namespace DellarteDellaGuerra.Domain.Tests.PoliticalMap
{
    public class PoliticalMapTerritoriesTests
    {
        private readonly FakeFeudalStructure _structure = new FakeFeudalStructure()
            .AddTitle("kingdom_england", TitleRank.King)
            .AddTitle("duchy_lancaster", TitleRank.Duke, "kingdom_england", "castle_pontefract")
            .AddTitle("county_lancashire", TitleRank.Earl, "duchy_lancaster", "town_lancaster")
            .AddTitle("barony_piel", TitleRank.Baron, "county_lancashire", "castle_piel")
            .AddTitle("barony_clitheroe", TitleRank.Baron, "duchy_lancaster", "castle_clitheroe")
            .AddTitle("county_kent", TitleRank.Earl, "kingdom_england", "town_canterbury");

        private readonly FakeTitleRepository _titles = new(
            new Title("kingdom_england", "England", TitleRank.King, "", "henry"),
            new Title("duchy_lancaster", "Duchy of Lancaster", TitleRank.Duke, "castle_pontefract", "henry"),
            new Title("county_lancashire", "County of Lancashire", TitleRank.Earl, "town_lancaster", "henry"),
            new Title("barony_piel", "Barony of Piel", TitleRank.Baron, "castle_piel", "harrington"),
            new Title("barony_clitheroe", "Barony of Clitheroe", TitleRank.Baron, "castle_clitheroe", "lacy"),
            new Title("county_kent", "County of Kent", TitleRank.Earl, "town_canterbury", "neville"));

        private PoliticalMapTerritories Territories() => new(_structure, _titles);

        [Theory]
        [InlineData("town_lancaster", "county_lancashire")]
        [InlineData("castle_piel", "county_lancashire")]
        [InlineData("town_canterbury", "county_kent")]
        [InlineData("castle_pontefract", "duchy_lancaster")]
        [InlineData("castle_clitheroe", "barony_clitheroe")]
        public void ASeatIsPaintedWithTheNearestCountyAboveItOrItsOwnTitleWhenThereIsNone(string seat, string territory)
        {
            Assert.Equal(territory, Territories().GetTerritoryOfSeat(seat));
        }

        [Fact]
        public void ASettlementThatSeatsNoTitleBelongsToNoTerritory()
        {
            Assert.Null(Territories().GetTerritoryOfSeat("hideout_forest"));
        }

        [Theory]
        [InlineData("county_lancashire", "county_lancashire")]
        [InlineData("barony_piel", "county_lancashire")]
        [InlineData("duchy_lancaster", "duchy_lancaster")]
        public void AHolderChangeRepaintsTheTerritoryItsSeatLiesIn(string titleId, string territory)
        {
            Assert.Equal(new[] { territory }, Territories().GetTerritoriesAffectedByTitle(titleId));
        }

        [Fact]
        public void ADukeChangingHandsLeavesTheCountiesBelowAlone()
        {
            Assert.DoesNotContain("county_lancashire", Territories().GetTerritoriesAffectedByTitle("duchy_lancaster"));
        }

        [Fact]
        public void ASeatlessCrownChangingHandsRepaintsNothing()
        {
            Assert.Empty(Territories().GetTerritoriesAffectedByTitle("kingdom_england"));
            Assert.Empty(Territories().GetTerritoriesAffectedByTitle("title_unknown"));
        }

        [Fact]
        public void SeatsResolveToDistinctTerritoriesInFirstSeenOrder()
        {
            Assert.Equal(
                new[] { "county_lancashire", "county_kent" },
                Territories().GetTerritoriesOfSeats(new[] { "castle_piel", "hideout_forest", "town_canterbury", "town_lancaster" }));
        }

        [Fact]
        public void AReattachedCountyIsAnsweredForWhereItNowSits()
        {
            PoliticalMapTerritories territories = Territories();
            _structure.Reattach("barony_clitheroe", "county_kent");

            Assert.Equal("county_kent", territories.GetTerritoryOfSeat("castle_clitheroe"));
        }
    }
}
