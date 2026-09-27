using DellarteDellaGuerra.Domain.Titles;
using DellarteDellaGuerra.Domain.Titles.Model;

namespace DellarteDellaGuerra.Domain.Tests.Titles
{
    public class AwardWonClaimUseCaseTests
    {
        // county (loser) ── barony_a (loser)
        //                └─ barony_b (third)
        // county_elsewhere (loser), outside the fought-for title
        private static (AwardWonClaimUseCase UseCase, FakeTitleRepository Titles) Setup(
            string? countyOccupant = null,
            bool winnerHasLeader = true)
        {
            var genealogy = new FakeGenealogy()
                .AddHero("loser_lord", "clan_loser")
                .AddHero("third_lord", "clan_third")
                .AddHero("winner_lord", "clan_winner");
            if (winnerHasLeader) genealogy.WithLeader("clan_winner", "winner_lord");

            var structure = new FakeFeudalStructure()
                .AddTitle("county", TitleRank.Count)
                .AddTitle("barony_a", TitleRank.Baron, parentTitleId: "county")
                .AddTitle("barony_b", TitleRank.Baron, parentTitleId: "county")
                .AddTitle("county_elsewhere", TitleRank.Count);

            var titles = new FakeTitleRepository(
                new Title("county", "County", TitleRank.Count, "seat_county", "loser_lord",
                    countyOccupant, countyOccupant is null ? null : 10f),
                new Title("barony_a", "Barony A", TitleRank.Baron, "seat_a", "loser_lord"),
                new Title("barony_b", "Barony B", TitleRank.Baron, "seat_b", "third_lord"),
                new Title("county_elsewhere", "Elsewhere", TitleRank.Count, "seat_elsewhere", "loser_lord"))
            {
                Genealogy = genealogy
            };

            return (new AwardWonClaimUseCase(titles, structure, genealogy), titles);
        }

        [Fact]
        public void Execute_GivesTheWinnerTheTitleAndTheDeJureTitlesTheLoserHeld()
        {
            var (useCase, titles) = Setup();

            useCase.Execute("county", "clan_winner", "clan_loser");

            Assert.Equal("winner_lord", titles.GetTitle("county")!.HolderHeroId);
            Assert.Equal("winner_lord", titles.GetTitle("barony_a")!.HolderHeroId);
        }

        [Fact]
        public void Execute_LeavesADeJureTitleAnotherClanHolds()
        {
            var (useCase, titles) = Setup();

            useCase.Execute("county", "clan_winner", "clan_loser");

            Assert.Equal("third_lord", titles.GetTitle("barony_b")!.HolderHeroId);
        }

        [Fact]
        public void Execute_LeavesTheLosersTitlesOutsideTheFoughtForTitle()
        {
            var (useCase, titles) = Setup();

            useCase.Execute("county", "clan_winner", "clan_loser");

            Assert.Equal("loser_lord", titles.GetTitle("county_elsewhere")!.HolderHeroId);
        }

        [Fact]
        public void Execute_ReturnsTheSeatsOfTheTitlesItMoved()
        {
            var (useCase, _) = Setup();

            var seats = useCase.Execute("county", "clan_winner", "clan_loser");

            Assert.Equal(new[] { "seat_county", "seat_a" }, seats);
        }

        [Fact]
        public void Execute_EndsTheContest_WhenTheWinnerOccupiedTheSeat()
        {
            var (useCase, titles) = Setup(countyOccupant: "clan_winner");

            useCase.Execute("county", "clan_winner", "clan_loser");

            Assert.False(titles.GetTitle("county")!.IsContested);
        }

        [Fact]
        public void Execute_KeepsAThirdPartysOccupation()
        {
            var (useCase, titles) = Setup(countyOccupant: "clan_third");

            useCase.Execute("county", "clan_winner", "clan_loser");

            Title county = titles.GetTitle("county")!;
            Assert.Equal("winner_lord", county.HolderHeroId);
            Assert.Equal("clan_third", county.OccupantClanId);
        }

        [Fact]
        public void Execute_MovesNothing_WhenTheWinnerHasNoLeader()
        {
            var (useCase, titles) = Setup(winnerHasLeader: false);

            var seats = useCase.Execute("county", "clan_winner", "clan_loser");

            Assert.Empty(seats);
            Assert.Equal("loser_lord", titles.GetTitle("county")!.HolderHeroId);
        }
    }
}
