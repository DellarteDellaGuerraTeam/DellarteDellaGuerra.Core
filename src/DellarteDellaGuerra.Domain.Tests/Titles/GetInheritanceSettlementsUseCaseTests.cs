using DellarteDellaGuerra.Domain.Titles;
using DellarteDellaGuerra.Domain.Titles.Model;

namespace DellarteDellaGuerra.Domain.Tests.Titles
{
    public class GetInheritanceSettlementsUseCaseTests
    {
        // duchy (house) ── county (house, by allegiance) ── barony (house, by allegiance)
        //               └─ county_follower (follower) ── barony_under_follower (house)
        // county_geographic (house), only nested under duchy in the de jure tree
        private static GetInheritanceSettlementsUseCase Setup(out FakeTitleRepository titles)
        {
            var genealogy = new FakeGenealogy()
                .AddHero("house_lord", "clan_house")
                .AddHero("follower_lord", "clan_follower");

            var structure = new FakeFeudalStructure()
                .AddTitle("region", TitleRank.Duke)
                .AddTitle("duchy", TitleRank.Duke, parentTitleId: "region")
                .AddTitle("county", TitleRank.Earl, parentTitleId: "region")
                .AddTitle("barony", TitleRank.Baron, parentTitleId: "region")
                .AddTitle("county_follower", TitleRank.Earl, parentTitleId: "region")
                .AddTitle("barony_under_follower", TitleRank.Baron, parentTitleId: "region")
                .AddTitle("county_geographic", TitleRank.Earl, parentTitleId: "duchy")
                .WithAllegiance("county", "duchy")
                .WithAllegiance("barony", "county")
                .WithAllegiance("county_follower", "duchy")
                .WithAllegiance("barony_under_follower", "county_follower")
                .WithAllegiance("county_geographic", "region");

            titles = new FakeTitleRepository(
                new Title("duchy", "Duchy", TitleRank.Duke, "seat_duchy", "house_lord"),
                new Title("county", "County", TitleRank.Earl, "seat_county", "house_lord"),
                new Title("barony", "Barony", TitleRank.Baron, "seat_barony", "house_lord"),
                new Title("county_follower", "Follower", TitleRank.Earl, "seat_follower", "follower_lord"),
                new Title("barony_under_follower", "Under Follower", TitleRank.Baron, "seat_under_follower", "house_lord"),
                new Title("county_geographic", "Geographic", TitleRank.Earl, "seat_geographic", "house_lord"))
            {
                Genealogy = genealogy
            };

            return new GetInheritanceSettlementsUseCase(titles, structure, genealogy);
        }

        [Fact]
        public void Execute_ReturnsTheSeatAndTheSeatsTheHouseHoldsUnderItByAllegiance()
        {
            var useCase = Setup(out _);

            var result = useCase.Execute("duchy");

            Assert.Equal(new[] { "seat_duchy", "seat_county", "seat_barony" }, result);
        }

        [Fact]
        public void Execute_LeavesOutAFollowersTitleAndEverythingBelowIt()
        {
            var useCase = Setup(out _);

            var result = useCase.Execute("duchy");

            Assert.DoesNotContain("seat_follower", result);
            Assert.DoesNotContain("seat_under_follower", result);
        }

        [Fact]
        public void Execute_IgnoresTheDeJureTree()
        {
            var useCase = Setup(out _);

            var result = useCase.Execute("duchy");

            Assert.DoesNotContain("seat_geographic", result);
        }

        [Fact]
        public void Execute_DisputesOnlyTheEstate_ForATitleWithNothingHeldUnderIt()
        {
            var useCase = Setup(out _);

            var result = useCase.Execute("barony");

            Assert.Equal(new[] { "seat_barony" }, result);
        }

        [Fact]
        public void Execute_ReturnsOnlyTheSeat_ForAnUnheldTitle()
        {
            var useCase = Setup(out var titles);
            titles.SaveTitle(new Title("duchy", "Duchy", TitleRank.Duke, "seat_duchy", null));

            var result = useCase.Execute("duchy");

            Assert.Equal(new[] { "seat_duchy" }, result);
        }

        [Fact]
        public void Execute_ReturnsAnEmptyList_ForAnUnknownTitle()
        {
            var useCase = Setup(out _);

            Assert.Empty(useCase.Execute("nonexistent"));
        }
    }
}
