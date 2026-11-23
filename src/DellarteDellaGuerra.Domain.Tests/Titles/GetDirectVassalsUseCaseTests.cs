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
            //   duchy_1 (clan_duke) -> county_1 (clan_earl1)
            //   duchy_2 (vacant)    -> county_2 (clan_earl2)
            var structure = new FakeFeudalStructure()
                .AddTitle("kingdom_k", TitleRank.King)
                .AddTitle("duchy_1", TitleRank.Duke, "kingdom_k")
                .AddTitle("duchy_2", TitleRank.Duke, "kingdom_k")
                .AddTitle("county_1", TitleRank.Earl, "duchy_1")
                .AddTitle("county_2", TitleRank.Earl, "duchy_2");

            var genealogy = new FakeGenealogy()
                .AddHero("clan_king", "clan_king")
                .AddHero("clan_duke", "clan_duke")
                .AddHero("clan_earl1", "clan_earl1")
                .AddHero("clan_earl2", "clan_earl2");

            _titleRepository = new FakeTitleRepository(
                new Title("kingdom_k", "Kingdom", TitleRank.King, "s_k", "clan_king"),
                new Title("duchy_1", "Duchy 1", TitleRank.Duke, "s_d1", "clan_duke"),
                new Title("duchy_2", "Duchy 2", TitleRank.Duke, "s_d2", null),
                new Title("county_1", "County 1", TitleRank.Earl, "s_c1", "clan_earl1"),
                new Title("county_2", "County 2", TitleRank.Earl, "s_c2", "clan_earl2"))
            { Genealogy = genealogy };

            _useCase = new GetDirectVassalsUseCase(
                _titleRepository, structure, new SuzeraintyPolicy(_titleRepository, structure, genealogy), genealogy);
        }

        [Fact]
        public void Execute_ReturnsDukeAndDukelessEarl_AsDirectVassalsOfKing()
        {
            var vassals = _useCase.Execute("clan_king");

            Assert.Equal(2, vassals.Count);
            Assert.Contains("clan_duke", vassals);
            Assert.Contains("clan_earl2", vassals);
        }

        [Fact]
        public void Execute_DoesNotIncludeDukesEarls_AsDirectVassalsOfKing()
        {
            var vassals = _useCase.Execute("clan_king");

            Assert.DoesNotContain("clan_earl1", vassals);
        }

        [Fact]
        public void Execute_ReturnsEarl_AsDirectVassalOfDuke()
        {
            var vassals = _useCase.Execute("clan_duke");

            Assert.Equal(new[] { "clan_earl1" }, vassals);
        }
    }
}
