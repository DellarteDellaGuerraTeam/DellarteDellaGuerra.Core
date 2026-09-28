using DellarteDellaGuerra.Domain.Titles;
using DellarteDellaGuerra.Domain.Titles.Model;

namespace DellarteDellaGuerra.Domain.Tests.Titles
{
    public class AwardWonClaimUseCaseTests
    {
        // county (loser) ── barony_a (loser)
        //                └─ barony_b (third)
        // county_elsewhere (loser) ── barony_c (fourth), outside the fought-for title
        private static (AwardWonClaimUseCase UseCase, FakeTitleRepository Titles, SuzeraintyPolicy Suzerain) Setup(
            string? countyOccupant = null,
            bool winnerHasLeader = true)
        {
            var genealogy = new FakeGenealogy()
                .AddHero("loser_lord", "clan_loser")
                .AddHero("third_lord", "clan_third")
                .AddHero("fourth_lord", "clan_fourth")
                .AddHero("winner_lord", "clan_winner");
            if (winnerHasLeader) genealogy.WithLeader("clan_winner", "winner_lord");

            var structure = new FakeFeudalStructure()
                .AddTitle("county", TitleRank.Count)
                .AddTitle("barony_a", TitleRank.Baron, parentTitleId: "county")
                .AddTitle("barony_b", TitleRank.Baron, parentTitleId: "county")
                .AddTitle("county_elsewhere", TitleRank.Count)
                .AddTitle("barony_c", TitleRank.Baron, parentTitleId: "county_elsewhere");

            var titles = new FakeTitleRepository(
                new Title("county", "County", TitleRank.Count, "seat_county", "loser_lord",
                    countyOccupant, countyOccupant is null ? null : 10f),
                new Title("barony_a", "Barony A", TitleRank.Baron, "seat_a", "loser_lord"),
                new Title("barony_b", "Barony B", TitleRank.Baron, "seat_b", "third_lord"),
                new Title("county_elsewhere", "Elsewhere", TitleRank.Count, "seat_elsewhere", "loser_lord"),
                new Title("barony_c", "Barony C", TitleRank.Baron, "seat_c", "fourth_lord"))
            {
                Genealogy = genealogy
            };

            return (
                new AwardWonClaimUseCase(titles, structure, genealogy, new SuzeraintyPolicy(titles, structure, genealogy)),
                titles,
                new SuzeraintyPolicy(titles, structure, genealogy));
        }

        [Fact]
        public void Execute_GivesTheWinnerTheTitleAndTheDeJureTitlesTheLoserHeld()
        {
            var (useCase, titles, _) = Setup();

            useCase.Execute("county", "clan_winner", "clan_loser", 10f);

            Assert.Equal("winner_lord", titles.GetTitle("county")!.HolderHeroId);
            Assert.Equal("winner_lord", titles.GetTitle("barony_a")!.HolderHeroId);
        }

        [Fact]
        public void Execute_LeavesADeJureTitleAnotherClanHolds()
        {
            var (useCase, titles, _) = Setup();

            useCase.Execute("county", "clan_winner", "clan_loser", 10f);

            Assert.Equal("third_lord", titles.GetTitle("barony_b")!.HolderHeroId);
        }

        [Fact]
        public void Execute_MakesTheWinnerTheLiegeOfAVassalItDidNotDispossess()
        {
            var (useCase, _, suzerain) = Setup();
            Assert.Equal("clan_loser", suzerain.GetSuzerain("clan_third"));

            useCase.Execute("county", "clan_winner", "clan_loser", 10f);

            Assert.Equal("clan_winner", suzerain.GetSuzerain("clan_third"));
        }

        [Fact]
        public void Execute_LeavesTheLoserTheVassalsOutsideTheFoughtForTitle()
        {
            var (useCase, _, suzerain) = Setup();

            useCase.Execute("county", "clan_winner", "clan_loser", 10f);

            Assert.Equal("clan_loser", suzerain.GetSuzerain("clan_fourth"));
        }

        [Fact]
        public void Execute_LeavesTheLosersTitlesOutsideTheFoughtForTitle()
        {
            var (useCase, titles, _) = Setup();

            useCase.Execute("county", "clan_winner", "clan_loser", 10f);

            Assert.Equal("loser_lord", titles.GetTitle("county_elsewhere")!.HolderHeroId);
        }

        [Fact]
        public void Execute_ReturnsTheSeatsOfTheTitlesItMoved()
        {
            var (useCase, _, _) = Setup();

            var seats = useCase.Execute("county", "clan_winner", "clan_loser", 10f);

            Assert.Equal(new[] { "seat_county", "seat_a" }, seats.MovedSeatIds);
        }

        [Fact]
        public void Execute_EndsTheContest_WhenTheWinnerOccupiedTheSeat()
        {
            var (useCase, titles, _) = Setup(countyOccupant: "clan_winner");

            useCase.Execute("county", "clan_winner", "clan_loser", 10f);

            Assert.False(titles.GetTitle("county")!.IsContested);
        }

        [Fact]
        public void Execute_KeepsAThirdPartysOccupation()
        {
            var (useCase, titles, _) = Setup(countyOccupant: "clan_third");

            useCase.Execute("county", "clan_winner", "clan_loser", 10f);

            Title county = titles.GetTitle("county")!;
            Assert.Equal("winner_lord", county.HolderHeroId);
            Assert.Equal("clan_third", county.OccupantClanId);
        }

        [Fact]
        public void Execute_MovesNothing_WhenTheWinnerHasNoLeader()
        {
            var (useCase, titles, _) = Setup(winnerHasLeader: false);

            var seats = useCase.Execute("county", "clan_winner", "clan_loser", 10f);

            Assert.Empty(seats.MovedSeatIds);
            Assert.Equal("loser_lord", titles.GetTitle("county")!.HolderHeroId);
        }

        // kingdom_scotland (scots_king) ── county (loser) ── barony_a (loser)
        //                               │                 ├─ barony_b (third)
        //                               │                 └─ barony_split (split)
        //                               └─ county_split (split)
        // kingdom_england (england_king) ── winner_title (winner, of the given rank)
        private static (AwardWonClaimUseCase UseCase, FakeFeudalStructure Structure, SuzeraintyPolicy Suzerain)
            SetupAcrossTheBorder(TitleRank winnerRank = TitleRank.Count, string winnerRealm = "kingdom_england")
        {
            var genealogy = new FakeGenealogy()
                .AddHero("scots_king", "clan_scots")
                .AddHero("england_king", "clan_england")
                .AddHero("loser_lord", "clan_loser")
                .AddHero("third_lord", "clan_third")
                .AddHero("split_lord", "clan_split")
                .AddHero("winner_lord", "clan_winner")
                .WithLeader("clan_winner", "winner_lord");

            var structure = new FakeFeudalStructure()
                .AddTitle("kingdom_scotland", TitleRank.King)
                .AddTitle("county", TitleRank.Count, parentTitleId: "kingdom_scotland")
                .AddTitle("barony_a", TitleRank.Baron, parentTitleId: "county")
                .AddTitle("barony_b", TitleRank.Baron, parentTitleId: "county")
                .AddTitle("barony_split", TitleRank.Baron, parentTitleId: "county")
                .AddTitle("county_split", TitleRank.Count, parentTitleId: "kingdom_scotland")
                .AddTitle("kingdom_england", TitleRank.King)
                .AddTitle("winner_title", winnerRank, parentTitleId: winnerRealm);

            var titles = new FakeTitleRepository(
                new Title("kingdom_scotland", "Scotland", TitleRank.King, "seat_scotland", "scots_king"),
                new Title("county", "County", TitleRank.Count, "seat_county", "loser_lord"),
                new Title("barony_a", "Barony A", TitleRank.Baron, "seat_a", "loser_lord"),
                new Title("barony_b", "Barony B", TitleRank.Baron, "seat_b", "third_lord"),
                new Title("barony_split", "Barony Split", TitleRank.Baron, "seat_bs", "split_lord"),
                new Title("county_split", "County Split", TitleRank.Count, "seat_cs", "split_lord"),
                new Title("kingdom_england", "England", TitleRank.King, "seat_england", "england_king"),
                new Title("winner_title", "Winner", winnerRank, "seat_winner", "winner_lord"))
            {
                Genealogy = genealogy
            };

            var suzerain = new SuzeraintyPolicy(titles, structure, genealogy);
            return (new AwardWonClaimUseCase(titles, structure, genealogy, suzerain), structure, suzerain);
        }

        [Fact]
        public void Execute_AttachesTheWonTitleUnderTheWinnersKing_WhenItRanksAsHighAsTheWinnersTitle()
        {
            var (useCase, structure, suzerain) = SetupAcrossTheBorder(TitleRank.Count);

            useCase.Execute("county", "clan_winner", "clan_loser", 10f);

            Assert.Equal("kingdom_england", structure.GetDeJureSuzerainTitleId("county"));
            Assert.Equal("clan_england", suzerain.GetSuzerain("clan_winner"));
        }

        [Fact]
        public void Execute_AttachesTheWonTitleUnderTheWinnersTitle_WhenItRanksLower()
        {
            var (useCase, structure, _) = SetupAcrossTheBorder(TitleRank.Duke);

            useCase.Execute("county", "clan_winner", "clan_loser", 10f);

            Assert.Equal("winner_title", structure.GetDeJureSuzerainTitleId("county"));
        }

        [Fact]
        public void Execute_BringsTheVassalsWhosePrimaryTitleItCarriesIntoTheWinnersRealm()
        {
            var (useCase, _, suzerain) = SetupAcrossTheBorder();

            var award = useCase.Execute("county", "clan_winner", "clan_loser", 10f);

            Assert.Equal(new[] { "clan_third" }, award.ClansJoiningWinnersRealm);
            Assert.Equal("clan_winner", suzerain.GetSuzerain("clan_third"));
            Assert.Equal("clan_scots", suzerain.GetSuzerain("clan_split"));
        }

        [Fact]
        public void Execute_LeavesTheTitleInItsRealm_WhenTheWinnerIsOfTheSameRealm()
        {
            var (useCase, structure, _) = SetupAcrossTheBorder(winnerRealm: "kingdom_scotland");

            var award = useCase.Execute("county", "clan_winner", "clan_loser", 10f);

            Assert.Equal("kingdom_scotland", structure.GetDeJureSuzerainTitleId("county"));
            Assert.Empty(award.ClansJoiningWinnersRealm);
        }

        [Fact]
        public void Execute_LeavesAWonKingdomARealmOfItsOwn()
        {
            var (useCase, structure, _) = SetupAcrossTheBorder();

            var award = useCase.Execute("kingdom_scotland", "clan_winner", "clan_scots", 10f);

            Assert.Null(structure.GetDeJureSuzerainTitleId("kingdom_scotland"));
            Assert.Empty(award.ClansJoiningWinnersRealm);
        }
    }
}
