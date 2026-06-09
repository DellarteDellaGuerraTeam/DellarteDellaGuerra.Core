using DellarteDellaGuerra.Domain.Titles;
using DellarteDellaGuerra.Domain.Titles.Model;

namespace DellarteDellaGuerra.Domain.Tests.Titles
{
    public class GetSuzerainUseCaseTests
    {
        private static FakeFeudalStructure CreateStructure() => new FakeFeudalStructure()
            .AddTitle("kingdom_k", TitleRank.King)
            .AddTitle("duchy_d", TitleRank.Duke, "kingdom_k")
            .AddTitle("county_c", TitleRank.Count, "duchy_d")
            .AddTitle("barony_b", TitleRank.Baron, "county_c");

        [Fact]
        public void Execute_ReturnsCount_ForBaron()
        {
            var titleRepository = new FakeTitleRepository(
                new Title("kingdom_k", "Kingdom", TitleRank.King, "s_k", "clan_king"),
                new Title("duchy_d", "Duchy", TitleRank.Duke, "s_d", "clan_duke"),
                new Title("county_c", "County", TitleRank.Count, "s_c", "clan_count"),
                new Title("barony_b", "Barony", TitleRank.Baron, "s_b", "clan_baron"));
            var useCase = new GetSuzerainUseCase(titleRepository, CreateStructure());

            Assert.Equal("clan_count", useCase.Execute("clan_baron"));
        }

        [Fact]
        public void Execute_SkipsVacantCounty_ReturnsDuke()
        {
            var titleRepository = new FakeTitleRepository(
                new Title("kingdom_k", "Kingdom", TitleRank.King, "s_k", "clan_king"),
                new Title("duchy_d", "Duchy", TitleRank.Duke, "s_d", "clan_duke"),
                new Title("county_c", "County", TitleRank.Count, "s_c", null),
                new Title("barony_b", "Barony", TitleRank.Baron, "s_b", "clan_baron"));
            var useCase = new GetSuzerainUseCase(titleRepository, CreateStructure());

            Assert.Equal("clan_duke", useCase.Execute("clan_baron"));
        }

        [Fact]
        public void Execute_ReturnsKing_ForCountWithoutDuke()
        {
            var titleRepository = new FakeTitleRepository(
                new Title("kingdom_k", "Kingdom", TitleRank.King, "s_k", "clan_king"),
                new Title("duchy_d", "Duchy", TitleRank.Duke, "s_d", null),
                new Title("county_c", "County", TitleRank.Count, "s_c", "clan_count"));
            var useCase = new GetSuzerainUseCase(titleRepository, CreateStructure());

            Assert.Equal("clan_king", useCase.Execute("clan_count"));
        }

        [Fact]
        public void Execute_ReturnsNull_ForKing()
        {
            var titleRepository = new FakeTitleRepository(
                new Title("kingdom_k", "Kingdom", TitleRank.King, "s_k", "clan_king"));
            var useCase = new GetSuzerainUseCase(titleRepository, CreateStructure());

            Assert.Null(useCase.Execute("clan_king"));
        }

        [Fact]
        public void Execute_ReturnsNull_ForUntitledClan()
        {
            var titleRepository = new FakeTitleRepository(
                new Title("kingdom_k", "Kingdom", TitleRank.King, "s_k", "clan_king"));
            var useCase = new GetSuzerainUseCase(titleRepository, CreateStructure());

            Assert.Null(useCase.Execute("clan_untitled"));
        }

        [Fact]
        public void GetHighestRank_ReturnsHighestRankOrNull()
        {
            var titleRepository = new FakeTitleRepository(
                new Title("duchy_d", "Duchy", TitleRank.Duke, "s_d", "clan_duke"),
                new Title("county_c", "County", TitleRank.Count, "s_c", "clan_duke"));
            var useCase = new GetSuzerainUseCase(titleRepository, CreateStructure());

            Assert.Equal(TitleRank.Duke, useCase.GetHighestRank("clan_duke"));
            Assert.Null(useCase.GetHighestRank("clan_untitled"));
        }
    }
}
