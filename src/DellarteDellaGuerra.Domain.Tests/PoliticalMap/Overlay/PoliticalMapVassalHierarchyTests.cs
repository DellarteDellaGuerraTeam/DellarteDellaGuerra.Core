using DellarteDellaGuerra.Domain.PoliticalMap.Overlay;
using DellarteDellaGuerra.Domain.Tests.Titles;
using DellarteDellaGuerra.Domain.Titles.Model;

namespace DellarteDellaGuerra.Domain.Tests.PoliticalMap.Overlay
{
    public class PoliticalMapVassalHierarchyTests
    {
        // England: King Lancaster; Duke Beaufort; Count Stanley under the duchy; Baron Tudor
        // under Stanley's county; Count Neville straight under the crown.
        private readonly FakeFeudalStructure _structure = new FakeFeudalStructure()
            .AddTitle("kingdom_england", TitleRank.King)
            .AddTitle("duchy_lancaster", TitleRank.Duke, "kingdom_england")
            .AddTitle("county_lancashire", TitleRank.Count, "duchy_lancaster")
            .AddTitle("barony_lathom", TitleRank.Baron, "county_lancashire")
            .AddTitle("county_kent", TitleRank.Count, "kingdom_england");

        private readonly FakeGenealogy _genealogy = new FakeGenealogy()
            .AddHero("henry", "clan_lancaster")
            .AddHero("somerset", "clan_beaufort")
            .AddHero("stanley", "clan_stanley")
            .AddHero("tudor", "clan_tudor")
            .AddHero("neville", "clan_neville")
            .AddHero("percy", "clan_percy")
            .AddHero("douglas", "clan_douglas");

        private readonly FakeTitleRepository _titles = new(
            new Title("kingdom_england", "England", TitleRank.King, "", "henry"),
            new Title("duchy_lancaster", "Duchy of Lancaster", TitleRank.Duke, "", "somerset"),
            new Title("county_lancashire", "Lancashire", TitleRank.Count, "", "stanley"),
            new Title("barony_lathom", "Lathom", TitleRank.Baron, "", "tudor"),
            new Title("county_kent", "Kent", TitleRank.Count, "", "neville"));

        private readonly FakeWorld _world = new();

        private PoliticalMapVassalHierarchy Hierarchy() => new(_structure, _titles, _genealogy, _world);

        [Fact]
        public void EachClanAnswersToTheHolderOfTheFirstTitleAboveItsHighest()
        {
            PoliticalMapVassalHierarchy hierarchy = Hierarchy();

            Assert.Equal("clan_lancaster", hierarchy.LiegeOf("clan_beaufort"));
            Assert.Equal("clan_lancaster", hierarchy.LiegeOf("clan_neville"));
            Assert.Equal("clan_beaufort", hierarchy.LiegeOf("clan_stanley"));
            Assert.Equal("clan_stanley", hierarchy.LiegeOf("clan_tudor"));
            Assert.Equal(new[] { "clan_tudor", "clan_stanley", "clan_beaufort", "clan_lancaster" }, hierarchy.ChainOf("clan_tudor"));
        }

        [Fact]
        public void TheRulerAndAKingdomlessClanAnswerToNoOne()
        {
            _world.Kingdoms.Remove("clan_douglas");
            PoliticalMapVassalHierarchy hierarchy = Hierarchy();

            Assert.Null(hierarchy.LiegeOf("clan_lancaster"));
            Assert.Null(hierarchy.LiegeOf("clan_douglas"));
            Assert.Equal(new[] { "clan_douglas" }, hierarchy.ChainOf("clan_douglas"));
            Assert.Equal("clan_douglas", hierarchy.RegionHolderOf("clan_douglas", PoliticalMapOwnerLevel.Vassals));
            Assert.Equal("clan_douglas", hierarchy.RegionHolderOf("clan_douglas", PoliticalMapOwnerLevel.SubVassals));
        }

        [Fact]
        public void AClanHoldingNoTitleAnswersToItsRulerAsADirectVassal()
        {
            PoliticalMapVassalHierarchy hierarchy = Hierarchy();

            Assert.Null(hierarchy.HighestTitleOf("clan_percy"));
            Assert.Equal("clan_lancaster", hierarchy.LiegeOf("clan_percy"));
            Assert.Equal("clan_percy", hierarchy.RegionHolderOf("clan_percy", PoliticalMapOwnerLevel.Vassals));
        }

        [Fact]
        public void FarOutLandGoesToTheRulersDirectVassalAndCloseInToThatVassalsOwnVassal()
        {
            PoliticalMapVassalHierarchy hierarchy = Hierarchy();

            Assert.Equal("clan_lancaster", hierarchy.RegionHolderOf("clan_lancaster", PoliticalMapOwnerLevel.Vassals));
            Assert.Equal("clan_beaufort", hierarchy.RegionHolderOf("clan_beaufort", PoliticalMapOwnerLevel.Vassals));
            Assert.Equal("clan_beaufort", hierarchy.RegionHolderOf("clan_stanley", PoliticalMapOwnerLevel.Vassals));
            Assert.Equal("clan_beaufort", hierarchy.RegionHolderOf("clan_tudor", PoliticalMapOwnerLevel.Vassals));

            Assert.Equal("clan_lancaster", hierarchy.RegionHolderOf("clan_lancaster", PoliticalMapOwnerLevel.SubVassals));
            Assert.Equal("clan_beaufort", hierarchy.RegionHolderOf("clan_beaufort", PoliticalMapOwnerLevel.SubVassals));
            Assert.Equal("clan_stanley", hierarchy.RegionHolderOf("clan_stanley", PoliticalMapOwnerLevel.SubVassals));
            Assert.Equal("clan_stanley", hierarchy.RegionHolderOf("clan_tudor", PoliticalMapOwnerLevel.SubVassals));
            Assert.Equal("clan_neville", hierarchy.RegionHolderOf("clan_neville", PoliticalMapOwnerLevel.SubVassals));
        }

        [Fact]
        public void AClanHoldingTheTitleAboveItsOwnAnswersToTheNextHolderUp()
        {
            // Stanley also holds the duchy: he answers to the king, and Tudor still to Stanley.
            _titles.SaveTitle(new Title("duchy_lancaster", "Duchy of Lancaster", TitleRank.Duke, "", "stanley"));
            PoliticalMapVassalHierarchy hierarchy = Hierarchy();

            Assert.Equal("duchy_lancaster", hierarchy.HighestTitleOf("clan_stanley")!.Id);
            Assert.Equal("clan_lancaster", hierarchy.LiegeOf("clan_stanley"));
            Assert.Equal("clan_stanley", hierarchy.LiegeOf("clan_tudor"));
        }

        [Fact]
        public void TheWalkUpSkipsTitlesTheClanItselfHoldsAboveItsHighest()
        {
            // Two counts in a row, both Stanley's: the first title another clan holds is the duchy.
            _structure.AddTitle("county_cheshire", TitleRank.Count, "county_lancashire");
            _genealogy.AddHero("stanley_2", "clan_stanley");
            _titles.SaveTitle(new Title("county_cheshire", "Cheshire", TitleRank.Count, "", "stanley_2"));
            PoliticalMapVassalHierarchy hierarchy = Hierarchy();

            Assert.Equal("county_cheshire", hierarchy.HighestTitleOf("clan_stanley")!.Id);
            Assert.Equal("clan_beaufort", hierarchy.LiegeOf("clan_stanley"));
        }

        [Fact]
        public void AnOccupantHoldsTheTitleInsteadOfItsHolder()
        {
            _titles.SaveTitle(_titles.GetTitle("duchy_lancaster")!.WithOccupant("clan_neville", 3f));
            PoliticalMapVassalHierarchy hierarchy = Hierarchy();

            Assert.Equal("clan_neville", hierarchy.LiegeOf("clan_stanley"));
            Assert.Equal("duchy_lancaster", hierarchy.HighestTitleOf("clan_neville")!.Id);
            Assert.Null(hierarchy.HighestTitleOf("clan_beaufort"));
            Assert.Equal("clan_lancaster", hierarchy.LiegeOf("clan_beaufort"));
        }

        [Fact]
        public void ALiegeSwornToAnotherKingdomIsReplacedByTheClansOwnRuler()
        {
            // Beaufort holds the duchy but sits in Scotland, ruled by Douglas: Stanley stays in England.
            _world.Kingdoms["clan_beaufort"] = "kingdom_scotland";
            PoliticalMapVassalHierarchy hierarchy = Hierarchy();

            Assert.Equal("clan_lancaster", hierarchy.LiegeOf("clan_stanley"));
            Assert.Equal("clan_douglas", hierarchy.LiegeOf("clan_beaufort"));
            Assert.Equal("clan_stanley", hierarchy.RegionHolderOf("clan_stanley", PoliticalMapOwnerLevel.Vassals));
            Assert.Equal("clan_beaufort", hierarchy.RegionHolderOf("clan_beaufort", PoliticalMapOwnerLevel.Vassals));
        }

        [Fact]
        public void ALoopUpTheTitlesIsCutToTheClanAndItsRuler()
        {
            // Two counties each under the other: Neville and Percy would answer to each other.
            _structure.AddTitle("county_york", TitleRank.Count, "county_durham")
                .AddTitle("county_durham", TitleRank.Count, "county_york");
            _titles.SaveTitle(new Title("county_kent", "Kent", TitleRank.Count, "", "henry"));
            _titles.SaveTitle(new Title("county_york", "York", TitleRank.Count, "", "neville"));
            _titles.SaveTitle(new Title("county_durham", "Durham", TitleRank.Count, "", "percy"));
            PoliticalMapVassalHierarchy hierarchy = Hierarchy();

            Assert.Equal("clan_percy", hierarchy.LiegeOf("clan_neville"));
            Assert.Equal("clan_neville", hierarchy.LiegeOf("clan_percy"));
            Assert.Equal(new[] { "clan_neville", "clan_lancaster" }, hierarchy.ChainOf("clan_neville"));
            Assert.Equal("clan_neville", hierarchy.RegionHolderOf("clan_neville", PoliticalMapOwnerLevel.Vassals));
        }

        [Fact]
        public void AClansHighestTitleIsItsHighestRanked()
        {
            _genealogy.AddHero("somerset_2", "clan_beaufort");
            _titles.SaveTitle(new Title("county_kent", "Kent", TitleRank.Count, "", "somerset_2"));
            PoliticalMapVassalHierarchy hierarchy = Hierarchy();

            Assert.Equal("Duchy of Lancaster", hierarchy.HighestTitleOf("clan_beaufort")!.Name);
            Assert.Equal("England", hierarchy.HighestTitleOf("clan_lancaster")!.Name);
        }

        [Fact]
        public void TheRulersLandUsesItsHighestControlledLandedTitleWithoutChangingItsRoyalRank()
        {
            _titles.SaveTitle(new Title("duchy_lancaster", "Duchy of Lancaster", TitleRank.Duke, "", "henry"));
            _titles.SaveTitle(new Title("county_kent", "Kent", TitleRank.Count, "", "henry"));
            PoliticalMapVassalHierarchy hierarchy = Hierarchy();

            Assert.Equal("duchy_lancaster", hierarchy.HighestLandTitleOf("clan_lancaster")!.Id);
            Assert.Equal("kingdom_england", hierarchy.HighestTitleOf("clan_lancaster")!.Id);
            Assert.Null(hierarchy.LiegeOf("clan_lancaster"));

            _titles.SaveTitle(_titles.GetTitle("duchy_lancaster")!.WithOccupant("clan_beaufort", 3f));
            hierarchy = Hierarchy();
            Assert.Equal("county_kent", hierarchy.HighestLandTitleOf("clan_lancaster")!.Id);
            Assert.Equal("duchy_lancaster", hierarchy.HighestLandTitleOf("clan_beaufort")!.Id);
        }

        [Fact]
        public void ACrownAloneDoesNotNameTheRulersEstate()
        {
            Assert.Null(Hierarchy().HighestLandTitleOf("clan_lancaster"));
        }

        private sealed class FakeWorld : IPoliticalMapWorld
        {
            public readonly Dictionary<string, string> Kingdoms = new()
            {
                ["clan_lancaster"] = "kingdom_england",
                ["clan_beaufort"] = "kingdom_england",
                ["clan_stanley"] = "kingdom_england",
                ["clan_tudor"] = "kingdom_england",
                ["clan_neville"] = "kingdom_england",
                ["clan_percy"] = "kingdom_england",
                ["clan_douglas"] = "kingdom_scotland"
            };

            public string? SeatOf(string settlementId) => settlementId;

            public string? OwnerClanOf(string settlementId) => null;

            public string? FactionOf(string settlementId) => null;

            public uint? ClanColor(string clanId) => null;

            public uint? FactionColor(string factionId) => null;

            public string? KingdomOfClan(string clanId) => Kingdoms.TryGetValue(clanId, out string? kingdom) ? kingdom : null;

            public string? RulerClanOf(string kingdomId) => kingdomId switch
            {
                "kingdom_england" => "clan_lancaster",
                "kingdom_scotland" => "clan_douglas",
                _ => null
            };
        }
    }
}
