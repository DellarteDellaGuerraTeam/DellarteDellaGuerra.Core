using DellarteDellaGuerra.Domain.Titles;
using DellarteDellaGuerra.Domain.Titles.Model;

namespace DellarteDellaGuerra.Domain.Tests.Titles
{
    public class GenerateInheritanceClaimsUseCaseTests
    {
        private readonly FakeClaimRepository _claimRepository = new();
        private readonly GenerateInheritanceClaimsUseCase _useCase;

        public GenerateInheritanceClaimsUseCaseTests()
        {
            var titleRepository = new FakeTitleRepository(
                new Title("county_a", "County A", TitleRank.Count, "s_a", "clan_deceased"),
                new Title("barony_b", "Barony B", TitleRank.Baron, "s_b", "clan_deceased"),
                new Title("county_c", "County C", TitleRank.Count, "s_c", "clan_other"));
            _useCase = new GenerateInheritanceClaimsUseCase(titleRepository, _claimRepository);
        }

        [Fact]
        public void Execute_CreatesClaimPerTitlePerHeir()
        {
            var claims = _useCase.Execute("clan_deceased", new[] { "clan_heir1", "clan_heir2" });

            Assert.Equal(4, claims.Count);
            Assert.Contains(claims, claim => claim.Id == "county_a:clan_heir1:inheritance");
            Assert.Contains(claims, claim => claim.Id == "county_a:clan_heir2:inheritance");
            Assert.Contains(claims, claim => claim.Id == "barony_b:clan_heir1:inheritance");
            Assert.Contains(claims, claim => claim.Id == "barony_b:clan_heir2:inheritance");
            Assert.All(claims, claim =>
            {
                Assert.Equal(ClaimStrength.Strong, claim.Strength);
                Assert.Equal(ClaimOrigin.Inheritance, claim.Origin);
            });
            Assert.Equal(4, _claimRepository.AllClaims.Count);
        }

        [Fact]
        public void Execute_SkipsExistingClaims()
        {
            _claimRepository.AddClaim(new Claim(
                "county_a:clan_heir1:inheritance", "clan_heir1", "county_a", ClaimStrength.Strong,
                ClaimOrigin.Inheritance));

            var claims = _useCase.Execute("clan_deceased", new[] { "clan_heir1" });

            var claim = Assert.Single(claims);
            Assert.Equal("barony_b:clan_heir1:inheritance", claim.Id);
            Assert.Equal(2, _claimRepository.AllClaims.Count);
        }

        [Fact]
        public void Execute_SkipsHeirEqualToDeceasedClan()
        {
            var claims = _useCase.Execute("clan_deceased", new[] { "clan_deceased" });

            Assert.Empty(claims);
            Assert.Empty(_claimRepository.AllClaims);
        }
    }
}
