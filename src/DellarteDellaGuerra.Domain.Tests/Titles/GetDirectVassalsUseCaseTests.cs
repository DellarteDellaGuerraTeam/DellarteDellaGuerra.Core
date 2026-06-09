using DellarteDellaGuerra.Domain.Titles;
using DellarteDellaGuerra.Domain.Titles.Model;

namespace DellarteDellaGuerra.Domain.Tests.Titles
{
    public class GetDirectVassalsUseCaseTests
    {
        private readonly FakeTitleRepository _titleRepository;
        private readonly GetDirectVassalsUseCase _useCase;

        public GetDirectVassalsUseCaseTests()
        {
            // kingdom_k (clan_king)
            //   duchy_1 (clan_duke) -> county_1 (clan_count1)
            //   duchy_2 (vacant)    -> county_2 (clan_count2)
            var structure = new FakeFeudalStructure()
                .AddTitle("kingdom_k", TitleRank.King)
                .AddTitle("duchy_1", TitleRank.Duke, "kingdom_k")
                .AddTitle("duchy_2", TitleRank.Duke, "kingdom_k")
                .AddTitle("county_1", TitleRank.Count, "duchy_1")
                .AddTitle("county_2", TitleRank.Count, "duchy_2");

            _titleRepository = new FakeTitleRepository(
                new Title("kingdom_k", "Kingdom", TitleRank.King, "s_k", "clan_king"),
                new Title("duchy_1", "Duchy 1", TitleRank.Duke, "s_d1", "clan_duke"),
                new Title("duchy_2", "Duchy 2", TitleRank.Duke, "s_d2", null),
                new Title("county_1", "County 1", TitleRank.Count, "s_c1", "clan_count1"),
                new Title("county_2", "County 2", TitleRank.Count, "s_c2", "clan_count2"));

            var getSuzerainUseCase = new GetSuzerainUseCase(_titleRepository, structure);
            _useCase = new GetDirectVassalsUseCase(_titleRepository, structure, getSuzerainUseCase);
        }

        [Fact]
        public void Execute_ReturnsDukeAndDukelessCount_AsDirectVassalsOfKing()
        {
            var vassals = _useCase.Execute("clan_king");

            Assert.Equal(2, vassals.Count);
            Assert.Contains("clan_duke", vassals);
            Assert.Contains("clan_count2", vassals);
        }

        [Fact]
        public void Execute_DoesNotIncludeDukesCounts_AsDirectVassalsOfKing()
        {
            var vassals = _useCase.Execute("clan_king");

            Assert.DoesNotContain("clan_count1", vassals);
        }

        [Fact]
        public void Execute_ReturnsCount_AsDirectVassalOfDuke()
        {
            var vassals = _useCase.Execute("clan_duke");

            Assert.Equal(new[] { "clan_count1" }, vassals);
        }
    }
}
