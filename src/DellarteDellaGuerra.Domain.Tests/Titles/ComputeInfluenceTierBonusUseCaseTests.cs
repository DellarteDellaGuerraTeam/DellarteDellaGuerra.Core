using DellarteDellaGuerra.Domain.Titles;
using DellarteDellaGuerra.Domain.Titles.Model;

namespace DellarteDellaGuerra.Domain.Tests.Titles
{
    public class ComputeInfluenceTierBonusUseCaseTests
    {
        [Theory]
        [InlineData(TitleRank.Baron, 0.5f)]
        [InlineData(TitleRank.Count, 1.0f)]
        [InlineData(TitleRank.Duke, 2.0f)]
        [InlineData(TitleRank.King, 3.0f)]
        [InlineData(TitleRank.Emperor, 4.0f)]
        public void Execute_ReturnsBonusForHighestRank(TitleRank rank, float expectedBonus)
        {
            var titleRepository = new FakeTitleRepository(
                new Title("title_low", "Low", TitleRank.Baron, "s_low", "clan_a"),
                new Title("title_high", "High", rank, "s_high", "clan_a"));
            var useCase = new ComputeInfluenceTierBonusUseCase(titleRepository);

            Assert.Equal(expectedBonus, useCase.Execute("clan_a"));
        }

        [Fact]
        public void Execute_ReturnsZero_ForUntitledClan()
        {
            var useCase = new ComputeInfluenceTierBonusUseCase(new FakeTitleRepository());

            Assert.Equal(0f, useCase.Execute("clan_untitled"));
        }
    }
}
