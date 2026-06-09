using DellarteDellaGuerra.Domain.Titles;
using DellarteDellaGuerra.Domain.Titles.Model;

namespace DellarteDellaGuerra.Domain.Tests.Titles
{
    public class EvaluateClaimUseCaseTests
    {
        private static Claim CreateClaim(string titleId) =>
            new("claim_1", "clan_claimant", titleId, ClaimStrength.Strong, ClaimOrigin.Conquest);

        [Fact]
        public void Execute_ReturnsTrue_WhenTitleHeldByAnotherClan()
        {
            var titleRepository = new FakeTitleRepository(
                new Title("county_a", "County", TitleRank.Count, "s_a", "clan_other"));
            var useCase = new EvaluateClaimUseCase(titleRepository);

            Assert.True(useCase.Execute(CreateClaim("county_a")));
        }

        [Fact]
        public void Execute_ReturnsFalse_WhenClaimantHoldsTitle()
        {
            var titleRepository = new FakeTitleRepository(
                new Title("county_a", "County", TitleRank.Count, "s_a", "clan_claimant"));
            var useCase = new EvaluateClaimUseCase(titleRepository);

            Assert.False(useCase.Execute(CreateClaim("county_a")));
        }

        [Fact]
        public void Execute_ReturnsFalse_WhenTitleVacant()
        {
            var titleRepository = new FakeTitleRepository(
                new Title("county_a", "County", TitleRank.Count, "s_a", null));
            var useCase = new EvaluateClaimUseCase(titleRepository);

            Assert.False(useCase.Execute(CreateClaim("county_a")));
        }

        [Fact]
        public void Execute_ReturnsFalse_WhenTitleMissing()
        {
            var useCase = new EvaluateClaimUseCase(new FakeTitleRepository());

            Assert.False(useCase.Execute(CreateClaim("county_missing")));
        }
    }
}
