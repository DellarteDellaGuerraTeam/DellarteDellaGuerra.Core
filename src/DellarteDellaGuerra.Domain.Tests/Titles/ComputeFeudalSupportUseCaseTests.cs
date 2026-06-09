using DellarteDellaGuerra.Domain.Titles;
using DellarteDellaGuerra.Domain.Titles.Model;

namespace DellarteDellaGuerra.Domain.Tests.Titles
{
    public class ComputeFeudalSupportUseCaseTests
    {
        private readonly ComputeFeudalSupportUseCase _useCase;

        public ComputeFeudalSupportUseCaseTests()
        {
            // duchy_1 (clan_duke): county_1 (seat s_c1, clan_count1), county_2 (seat s_c2, clan_peer)
            // duchy_2 (clan_outsider_duke): county_3 (seat s_c3, clan_outsider)
            var structure = new FakeFeudalStructure()
                .AddTitle("kingdom_k", TitleRank.King)
                .AddTitle("duchy_1", TitleRank.Duke, "kingdom_k")
                .AddTitle("duchy_2", TitleRank.Duke, "kingdom_k")
                .AddTitle("county_1", TitleRank.Count, "duchy_1", "s_c1")
                .AddTitle("county_2", TitleRank.Count, "duchy_1", "s_c2")
                .AddTitle("county_3", TitleRank.Count, "duchy_2", "s_c3");

            var titleRepository = new FakeTitleRepository(
                new Title("kingdom_k", "Kingdom", TitleRank.King, "s_k", "clan_king"),
                new Title("duchy_1", "Duchy 1", TitleRank.Duke, "s_d1", "clan_duke"),
                new Title("duchy_2", "Duchy 2", TitleRank.Duke, "s_d2", "clan_outsider_duke"),
                new Title("county_1", "County 1", TitleRank.Count, "s_c1", "clan_count1"),
                new Title("county_2", "County 2", TitleRank.Count, "s_c2", "clan_peer"),
                new Title("county_3", "County 3", TitleRank.Count, "s_c3", "clan_outsider"));

            var getSuzerainUseCase = new GetSuzerainUseCase(titleRepository, structure);
            _useCase = new ComputeFeudalSupportUseCase(titleRepository, structure, getSuzerainUseCase);
        }

        [Fact]
        public void Execute_ReturnsThree_ForDeJureSuzerainOfSettlement()
        {
            Assert.Equal(3.0f, _useCase.Execute("clan_duke", "s_c1"));
        }

        [Fact]
        public void Execute_ReturnsOnePointFive_ForSameDuchyPeer()
        {
            Assert.Equal(1.5f, _useCase.Execute("clan_peer", "s_c1"));
        }

        [Fact]
        public void Execute_ReturnsZeroPointTwo_ForOutsider()
        {
            Assert.Equal(0.2f, _useCase.Execute("clan_outsider", "s_c1"));
        }

        [Fact]
        public void Execute_ReturnsOne_ForUnmappedSettlement()
        {
            Assert.Equal(1.0f, _useCase.Execute("clan_duke", "s_unmapped"));
        }
    }
}
