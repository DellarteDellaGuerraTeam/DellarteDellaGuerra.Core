using DellarteDellaGuerra.Domain.Titles;
using DellarteDellaGuerra.Domain.Titles.Model;

namespace DellarteDellaGuerra.Domain.Tests.Titles
{
    public class ExecuteSuccessionUseCaseTests
    {
        private static Title CountyA(string? holderHeroId = "holder") =>
            new("county_a", "County A", TitleRank.Count, "seat_a", holderHeroId);

        private static Title BaronyB(string? holderHeroId = "holder") =>
            new("barony_b", "Barony B", TitleRank.Baron, "seat_b", holderHeroId);

        private static ExecuteSuccessionUseCase UseCase(
            FakeTitleRepository titleRepository,
            FakeGenealogy genealogy) =>
            new(titleRepository, genealogy, new FakeLoggerFactory());

        [Fact]
        public void PassesTheTitleToTheEldestSonRatherThanTheElderDaughter()
        {
            var titleRepository = new FakeTitleRepository(CountyA());
            var genealogy = new FakeGenealogy()
                .AddHero("holder", "clan_holder", childIds: new[] { "younger_son", "eldest_son", "daughter" })
                .AddHero("eldest_son", "clan_holder", age: 40f)
                .AddHero("younger_son", "clan_holder", age: 30f)
                .AddHero("daughter", "clan_holder", isFemale: true, age: 45f);

            UseCase(titleRepository, genealogy).Execute("holder");

            Assert.Equal("eldest_son", titleRepository.GetTitle("county_a")!.HolderHeroId);
        }

        [Fact]
        public void PassesTheTitleToTheEldestDaughterWhenNoSonSurvives()
        {
            var titleRepository = new FakeTitleRepository(CountyA());
            var genealogy = new FakeGenealogy()
                .AddHero("holder", "clan_holder", childIds: new[] { "younger_daughter", "elder_daughter" })
                .AddHero("elder_daughter", "clan_holder", isFemale: true, age: 30f)
                .AddHero("younger_daughter", "clan_holder", isFemale: true, age: 20f);

            UseCase(titleRepository, genealogy).Execute("holder");

            Assert.Equal("elder_daughter", titleRepository.GetTitle("county_a")!.HolderHeroId);
        }

        [Fact]
        public void PassesTheTitleToTheDaughterRatherThanToTheHoldersOwnBrother()
        {
            // Male preference asks for a son, not for the nearest man: the holder's own body
            // comes before his father's other children. The brother is deliberately the elder of
            // the two, so only the order of the two rules can explain the outcome.
            var titleRepository = new FakeTitleRepository(CountyA());
            var genealogy = new FakeGenealogy()
                .AddHero("father", "clan_holder", isAlive: false, childIds: new[] { "holder", "brother" })
                .AddHero("holder", "clan_holder", fatherId: "father", age: 45f,
                    childIds: new[] { "younger_daughter", "elder_daughter" })
                .AddHero("brother", "clan_holder", fatherId: "father", age: 50f)
                .AddHero("elder_daughter", "clan_holder", isFemale: true, age: 22f)
                .AddHero("younger_daughter", "clan_holder", isFemale: true, age: 18f);

            UseCase(titleRepository, genealogy).Execute("holder");

            Assert.Equal("elder_daughter", titleRepository.GetTitle("county_a")!.HolderHeroId);
        }

        [Fact]
        public void SkipsAHeroListedAsHisOwnParentRatherThanRecursingForever()
        {
            // The bloodlines are authored, so a hero can be given as his own parent — the shipped
            // content did exactly that to the Duke of Norfolk. Any parent-to-child index then
            // lists him among his own children. An unguarded walk recurses until the stack goes,
            // and a StackOverflowException cannot be caught: that is the game process dying on
            // every death, not a bad succession. The loop must be stepped over, not stopped at,
            // so the son declared after it still inherits.
            var titleRepository = new FakeTitleRepository(CountyA());
            var genealogy = new FakeGenealogy()
                .AddHero("holder", "clan_holder", age: 40f, childIds: new[] { "holder", "son" })
                .AddHero("son", "clan_holder", age: 20f);

            UseCase(titleRepository, genealogy).Execute("holder");

            Assert.Equal("son", titleRepository.GetTitle("county_a")!.HolderHeroId);
        }

        [Fact]
        public void PassesTheTitleToTheEldestBrotherWhenTheHolderIsChildless()
        {
            var titleRepository = new FakeTitleRepository(CountyA());
            var genealogy = new FakeGenealogy()
                .AddHero("father", "clan_holder", isAlive: false,
                    childIds: new[] { "holder", "younger_brother", "elder_brother" })
                .AddHero("holder", "clan_holder", fatherId: "father", age: 45f)
                .AddHero("elder_brother", "clan_holder", fatherId: "father", age: 50f)
                .AddHero("younger_brother", "clan_holder", fatherId: "father", age: 35f);

            UseCase(titleRepository, genealogy).Execute("holder");

            Assert.Equal("elder_brother", titleRepository.GetTitle("county_a")!.HolderHeroId);
        }

        [Fact]
        public void PassesTheTitleToTheClanHeadWhenNoKinSurvives()
        {
            var titleRepository = new FakeTitleRepository(CountyA());
            var genealogy = new FakeGenealogy()
                .WithLeader("clan_holder", "clan_head")
                .AddHero("holder", "clan_holder")
                .AddHero("clan_head", "clan_holder");

            UseCase(titleRepository, genealogy).Execute("holder");

            Assert.Equal("clan_head", titleRepository.GetTitle("county_a")!.HolderHeroId);
        }

        [Fact]
        public void LeavesTheTitleVacantRatherThanPassingItToADeadClanHead()
        {
            var titleRepository = new FakeTitleRepository(CountyA());
            var genealogy = new FakeGenealogy()
                .WithLeader("clan_holder", "holder")
                .AddHero("holder", "clan_holder", isAlive: false);

            UseCase(titleRepository, genealogy).Execute("holder");

            Assert.Null(titleRepository.GetTitle("county_a")!.HolderHeroId);
        }

        /// <summary>
        /// Representation: the predeceased eldest son's own son stands in his father's place
        /// and so inherits ahead of his uncle.
        /// </summary>
        [Fact]
        public void PassesTheTitleToAPredeceasedSonsChildAheadOfHisUncle()
        {
            var titleRepository = new FakeTitleRepository(CountyA());
            var genealogy = new FakeGenealogy()
                .AddHero("holder", "clan_holder", childIds: new[] { "eldest_son", "younger_son" })
                .AddHero("eldest_son", "clan_holder", isAlive: false, age: 40f, childIds: new[] { "grandson" })
                .AddHero("grandson", "clan_holder", age: 18f)
                .AddHero("younger_son", "clan_holder", age: 30f);

            UseCase(titleRepository, genealogy).Execute("holder");

            Assert.Equal("grandson", titleRepository.GetTitle("county_a")!.HolderHeroId);
        }

        [Fact]
        public void PassesEveryTitleTheDeceasedHeldToTheSameHeir()
        {
            var otherHoldersTitle = new Title(
                "county_c", "County C", TitleRank.Count, "seat_c", "another_holder");
            var titleRepository = new FakeTitleRepository(CountyA(), BaronyB(), otherHoldersTitle);
            var genealogy = new FakeGenealogy()
                .AddHero("holder", "clan_holder", childIds: new[] { "son" })
                .AddHero("son", "clan_holder", age: 25f);

            var results = UseCase(titleRepository, genealogy).Execute("holder");

            Assert.Equal("son", titleRepository.GetTitle("county_a")!.HolderHeroId);
            Assert.Equal("son", titleRepository.GetTitle("barony_b")!.HolderHeroId);
            Assert.Equal("another_holder", titleRepository.GetTitle("county_c")!.HolderHeroId);
            Assert.Equal(
                new[] { "barony_b", "county_a" },
                results.Select(result => result.TitleId).OrderBy(titleId => titleId));
            Assert.All(results, result => Assert.Equal("holder", result.PreviousHolderHeroId));
            Assert.All(results, result => Assert.Equal("son", result.NewHolderHeroId));
        }

        [Fact]
        public void DoesNothingWhenTheDeceasedHeldNoTitle()
        {
            var titleRepository = new FakeTitleRepository(CountyA());
            var genealogy = new FakeGenealogy()
                .AddHero("commoner", "clan_holder", childIds: new[] { "son" })
                .AddHero("son", "clan_holder");

            var results = UseCase(titleRepository, genealogy).Execute("commoner");

            Assert.Empty(results);
            Assert.Equal("holder", titleRepository.GetTitle("county_a")!.HolderHeroId);
        }
    }
}
