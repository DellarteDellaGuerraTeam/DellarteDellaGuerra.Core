using DellarteDellaGuerra.Domain.Titles;
using DellarteDellaGuerra.Domain.Titles.Model;

namespace DellarteDellaGuerra.Domain.Tests.Titles
{
    public class AccumulateTensionUseCaseTests
    {
        private readonly FakeTensionRepository _tensionRepository = new();

        [Theory]
        [InlineData(ClaimStrength.Weak, 0.5f)]
        [InlineData(ClaimStrength.Strong, 1.0f)]
        [InlineData(ClaimStrength.DeJure, 1.5f)]
        public void Execute_AccumulatesWithStrengthMultiplier(ClaimStrength strength, float expectedFirstDay)
        {
            var titleRepository = new FakeTitleRepository(
                new Title("county_a", "County", TitleRank.Count, "s_a", "clan_holder"));
            var useCase = new AccumulateTensionUseCase(_tensionRepository, titleRepository);
            var claim = new Claim("claim_1", "clan_claimant", "county_a", strength, ClaimOrigin.Conquest);

            var firstDay = useCase.Execute("clan_claimant", claim, 1.0f);
            var secondDay = useCase.Execute("clan_claimant", claim, 1.0f);

            Assert.Equal(expectedFirstDay, firstDay.Amount, 3);
            Assert.Equal(expectedFirstDay * 2, secondDay.Amount, 3);
            Assert.Equal(secondDay, _tensionRepository.GetTension("clan_claimant", "county_a"));
        }

        [Fact]
        public void Execute_ResetsTension_WhenClaimantBecomesHolder()
        {
            var titleRepository = new FakeTitleRepository(
                new Title("county_a", "County", TitleRank.Count, "s_a", "clan_claimant"));
            var useCase = new AccumulateTensionUseCase(_tensionRepository, titleRepository);
            _tensionRepository.SetTension(new FeudalTension("clan_claimant", "county_a", 42f));
            var claim = new Claim("claim_1", "clan_claimant", "county_a", ClaimStrength.Strong, ClaimOrigin.Conquest);

            var tension = useCase.Execute("clan_claimant", claim, 1.0f);

            Assert.Equal(0f, tension.Amount);
            Assert.Null(_tensionRepository.GetTension("clan_claimant", "county_a"));
        }
    }
}
