using DellarteDellaGuerra.Domain.Titles;
using DellarteDellaGuerra.Domain.Titles.Model;

namespace DellarteDellaGuerra.Domain.Tests.Titles
{
    public class EvaluateClaimUseCaseTests
    {
        private static Claim CreateClaim(string titleId) =>
            new("claim_1", "clan_claimant", titleId, ClaimStrength.Strong, ClaimOrigin.Conquest);

        private static Claim CreateBloodClaim(string titleId, string claimantHeroId, string claimantClanId) =>
            new("claim_1", claimantClanId, titleId, ClaimStrength.Strong, ClaimOrigin.Inheritance,
                claimantHeroId);

        private static FakeGenealogy CreateGenealogy() => new FakeGenealogy()
            .AddHero("clan_other", "clan_other")
            .AddHero("clan_claimant", "clan_claimant")
            .AddHero("holder", "clan_holder")
            .AddHero("brother", "clan_holder");

        [Fact]
        public void Execute_ReturnsTrue_WhenTitleHeldByAnotherClan()
        {
            var titleRepository = new FakeTitleRepository(
                new Title("county_a", "County", TitleRank.Count, "s_a", "clan_other"));
            var useCase = new EvaluateClaimUseCase(titleRepository, CreateGenealogy());

            Assert.True(useCase.Execute(CreateClaim("county_a")));
        }

        [Fact]
        public void Execute_ReturnsFalse_WhenClaimantHoldsTitle()
        {
            var titleRepository = new FakeTitleRepository(
                new Title("county_a", "County", TitleRank.Count, "s_a", "clan_claimant"));
            var useCase = new EvaluateClaimUseCase(titleRepository, CreateGenealogy());

            Assert.False(useCase.Execute(CreateClaim("county_a")));
        }

        [Fact]
        public void Execute_ReturnsTrue_WhenABloodClaimantSharesTheHoldersClan()
        {
            var titleRepository = new FakeTitleRepository(
                new Title("county_a", "County", TitleRank.Count, "s_a", "holder"));
            var useCase = new EvaluateClaimUseCase(titleRepository, CreateGenealogy());

            Assert.True(useCase.Execute(CreateBloodClaim("county_a", "brother", "clan_holder")));
        }

        [Fact]
        public void Execute_ReturnsFalse_WhenTheBloodClaimantIsTheHolderHimself()
        {
            var titleRepository = new FakeTitleRepository(
                new Title("county_a", "County", TitleRank.Count, "s_a", "holder"));
            var useCase = new EvaluateClaimUseCase(titleRepository, CreateGenealogy());

            Assert.False(useCase.Execute(CreateBloodClaim("county_a", "holder", "clan_holder")));
        }

        [Fact]
        public void Execute_ReturnsFalse_WhenTitleVacant()
        {
            var titleRepository = new FakeTitleRepository(
                new Title("county_a", "County", TitleRank.Count, "s_a", null));
            var useCase = new EvaluateClaimUseCase(titleRepository, CreateGenealogy());

            Assert.False(useCase.Execute(CreateClaim("county_a")));
        }

        [Fact]
        public void Execute_ReturnsFalse_WhenTitleMissing()
        {
            var useCase = new EvaluateClaimUseCase(new FakeTitleRepository(), CreateGenealogy());

            Assert.False(useCase.Execute(CreateClaim("county_missing")));
        }
    }
}
