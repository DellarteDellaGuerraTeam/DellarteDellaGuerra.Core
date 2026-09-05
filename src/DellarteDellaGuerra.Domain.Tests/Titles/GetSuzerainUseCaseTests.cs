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

        private static FakeGenealogy CreateGenealogy() => new FakeGenealogy()
            .AddHero("clan_king", "clan_king")
            .AddHero("clan_duke", "clan_duke")
            .AddHero("clan_count", "clan_count")
            .AddHero("clan_baron", "clan_baron");

        [Fact]
        public void Execute_ReturnsCount_ForBaron()
        {
            var genealogy = CreateGenealogy();
            var titleRepository = new FakeTitleRepository(
                new Title("kingdom_k", "Kingdom", TitleRank.King, "s_k", "clan_king"),
                new Title("duchy_d", "Duchy", TitleRank.Duke, "s_d", "clan_duke"),
                new Title("county_c", "County", TitleRank.Count, "s_c", "clan_count"),
                new Title("barony_b", "Barony", TitleRank.Baron, "s_b", "clan_baron"))
            { Genealogy = genealogy };
            var useCase = new GetSuzerainUseCase(titleRepository, CreateStructure(), genealogy);

            Assert.Equal("clan_count", useCase.Execute("clan_baron"));
        }

        [Fact]
        public void Execute_SkipsVacantCounty_ReturnsDuke()
        {
            var genealogy = CreateGenealogy();
            var titleRepository = new FakeTitleRepository(
                new Title("kingdom_k", "Kingdom", TitleRank.King, "s_k", "clan_king"),
                new Title("duchy_d", "Duchy", TitleRank.Duke, "s_d", "clan_duke"),
                new Title("county_c", "County", TitleRank.Count, "s_c", null),
                new Title("barony_b", "Barony", TitleRank.Baron, "s_b", "clan_baron"))
            { Genealogy = genealogy };
            var useCase = new GetSuzerainUseCase(titleRepository, CreateStructure(), genealogy);

            Assert.Equal("clan_duke", useCase.Execute("clan_baron"));
        }

        [Fact]
        public void Execute_ReturnsKing_ForCountWithoutDuke()
        {
            var genealogy = CreateGenealogy();
            var titleRepository = new FakeTitleRepository(
                new Title("kingdom_k", "Kingdom", TitleRank.King, "s_k", "clan_king"),
                new Title("duchy_d", "Duchy", TitleRank.Duke, "s_d", null),
                new Title("county_c", "County", TitleRank.Count, "s_c", "clan_count"))
            { Genealogy = genealogy };
            var useCase = new GetSuzerainUseCase(titleRepository, CreateStructure(), genealogy);

            Assert.Equal("clan_king", useCase.Execute("clan_count"));
        }

        [Fact]
        public void Execute_ReturnsNull_ForKing()
        {
            var genealogy = CreateGenealogy();
            var titleRepository = new FakeTitleRepository(
                new Title("kingdom_k", "Kingdom", TitleRank.King, "s_k", "clan_king"))
            { Genealogy = genealogy };
            var useCase = new GetSuzerainUseCase(titleRepository, CreateStructure(), genealogy);

            Assert.Null(useCase.Execute("clan_king"));
        }

        [Fact]
        public void Execute_ReturnsNull_ForUntitledClan()
        {
            var genealogy = CreateGenealogy();
            var titleRepository = new FakeTitleRepository(
                new Title("kingdom_k", "Kingdom", TitleRank.King, "s_k", "clan_king"))
            { Genealogy = genealogy };
            var useCase = new GetSuzerainUseCase(titleRepository, CreateStructure(), genealogy);

            Assert.Null(useCase.Execute("clan_untitled"));
        }

        [Fact]
        public void GetHighestRank_ReturnsHighestRankOrNull()
        {
            var genealogy = CreateGenealogy();
            var titleRepository = new FakeTitleRepository(
                new Title("duchy_d", "Duchy", TitleRank.Duke, "s_d", "clan_duke"),
                new Title("county_c", "County", TitleRank.Count, "s_c", "clan_duke"))
            { Genealogy = genealogy };
            var useCase = new GetSuzerainUseCase(titleRepository, CreateStructure(), genealogy);

            Assert.Equal(TitleRank.Duke, useCase.GetHighestRank("clan_duke"));
            Assert.Null(useCase.GetHighestRank("clan_untitled"));
        }
    }
}
