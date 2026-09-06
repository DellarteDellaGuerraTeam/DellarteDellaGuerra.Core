using DellarteDellaGuerra.Domain.Titles;
using DellarteDellaGuerra.Domain.Titles.Model;

namespace DellarteDellaGuerra.Domain.Tests.Titles
{
    public class GetDeJureSettlementsUseCaseTests
    {
        [Fact]
        public void Execute_ReturnsTheTitlesOwnSeat()
        {
            var feudalStructure = new FakeFeudalStructure().AddTitle("county_a", TitleRank.Count);
            var titleRepository = new FakeTitleRepository(
                new Title("county_a", "County A", TitleRank.Count, "seat_a", null));
            var useCase = new GetDeJureSettlementsUseCase(titleRepository, feudalStructure);

            var result = useCase.Execute("county_a");

            Assert.Equal(new[] { "seat_a" }, result);
        }

        [Fact]
        public void Execute_IncludesTheSeatsOfVassalsAndVassalsOfVassals()
        {
            var feudalStructure = new FakeFeudalStructure()
                .AddTitle("duchy", TitleRank.Duke)
                .AddTitle("county", TitleRank.Count, parentTitleId: "duchy")
                .AddTitle("barony", TitleRank.Baron, parentTitleId: "county");
            var titleRepository = new FakeTitleRepository(
                new Title("duchy", "Duchy", TitleRank.Duke, "seat_duchy", null),
                new Title("county", "County", TitleRank.Count, "seat_county", null),
                new Title("barony", "Barony", TitleRank.Baron, "seat_barony", null));
            var useCase = new GetDeJureSettlementsUseCase(titleRepository, feudalStructure);

            var result = useCase.Execute("duchy");

            Assert.Equal(new[] { "seat_duchy", "seat_county", "seat_barony" }, result);
        }

        [Fact]
        public void Execute_SkipsAVassalTitleAbsentFromTheRepository_ButStillCollectsItsOwnVassals()
        {
            var feudalStructure = new FakeFeudalStructure()
                .AddTitle("duchy", TitleRank.Duke)
                .AddTitle("county", TitleRank.Count, parentTitleId: "duchy")
                .AddTitle("barony", TitleRank.Baron, parentTitleId: "county");
            var titleRepository = new FakeTitleRepository(
                new Title("duchy", "Duchy", TitleRank.Duke, "seat_duchy", null),
                new Title("barony", "Barony", TitleRank.Baron, "seat_barony", null));
            var useCase = new GetDeJureSettlementsUseCase(titleRepository, feudalStructure);

            var result = useCase.Execute("duchy");

            Assert.Equal(new[] { "seat_duchy", "seat_barony" }, result);
        }

        [Fact]
        public void Execute_ReturnsAnEmptyList_ForATitleWithNoVassalsAndNoRepositoryRecord()
        {
            var feudalStructure = new FakeFeudalStructure();
            var titleRepository = new FakeTitleRepository();
            var useCase = new GetDeJureSettlementsUseCase(titleRepository, feudalStructure);

            var result = useCase.Execute("nonexistent");

            Assert.Empty(result);
        }
    }
}
