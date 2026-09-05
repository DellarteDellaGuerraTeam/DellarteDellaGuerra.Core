using DellarteDellaGuerra.Domain.Titles;
using DellarteDellaGuerra.Domain.Titles.Model;

namespace DellarteDellaGuerra.Domain.Tests.Titles
{
    public class GenerateBloodClaimsUseCaseTests
    {
        private readonly FakeClaimRepository _claimRepository = new();

        /// <summary>
        /// clan_holder holds county_a. Its head has a son who founded clan_son and a daughter
        /// who married into clan_daughter; each of them has children of their own.
        /// </summary>
        private static FakeGenealogy ThreeGenerations()
        {
            return new FakeGenealogy()
                .WithLeader("clan_holder", "holder")
                .AddHero("holder", "clan_holder", childIds: new[] { "son", "daughter" })
                .AddHero("son", "clan_son", childIds: new[] { "grandson", "granddaughter" })
                .AddHero("daughter", "clan_daughter", isFemale: true, childIds: new[] { "daughters_son" })
                .AddHero("grandson", "clan_son")
                .AddHero("granddaughter", "clan_son", isFemale: true)
                .AddHero("daughters_son", "clan_daughter");
        }

        private GenerateBloodClaimsUseCase UseCase(FakeGenealogy genealogy, params Title[] titles)
        {
            return new GenerateBloodClaimsUseCase(new FakeTitleRepository(titles), _claimRepository, genealogy);
        }

        private static Title CountyA(string? holderHeroId = "holder") =>
            new("county_a", "County A", TitleRank.Count, "seat_a", holderHeroId);

        private Claim? ClaimOf(string heroId) =>
            _claimRepository.AllClaims.FirstOrDefault(claim => claim.ClaimantHeroId == heroId);

        [Fact]
        public void GivesTheHoldersSonAStrongClaim()
        {
            UseCase(ThreeGenerations(), CountyA()).Execute();

            var claim = ClaimOf("son");
            Assert.NotNull(claim);
            Assert.Equal(ClaimStrength.Strong, claim!.Strength);
            Assert.Equal("county_a", claim.TitleId);
            Assert.Equal("clan_son", claim.ClaimantClanId);
            Assert.Equal("county_a:son:blood", claim.Id);
            Assert.Equal(ClaimOrigin.Inheritance, claim.Origin);
        }

        [Fact]
        public void GivesTheHoldersDaughterAWeakClaim()
        {
            UseCase(ThreeGenerations(), CountyA()).Execute();

            Assert.Equal(ClaimStrength.Weak, ClaimOf("daughter")?.Strength);
        }

        [Fact]
        public void DegradesAStrongClaimToWeakForTheSon()
        {
            UseCase(ThreeGenerations(), CountyA()).Execute();

            Assert.Equal(ClaimStrength.Weak, ClaimOf("grandson")?.Strength);
        }

        [Fact]
        public void DegradesAStrongClaimToWeakForTheDaughter()
        {
            UseCase(ThreeGenerations(), CountyA()).Execute();

            Assert.Equal(ClaimStrength.Weak, ClaimOf("granddaughter")?.Strength);
        }

        [Fact]
        public void PassesNothingOnFromAWeakClaim()
        {
            UseCase(ThreeGenerations(), CountyA()).Execute();

            Assert.Null(ClaimOf("daughters_son"));
        }

        [Fact]
        public void IgnoresDescendantsStillInTheHoldingClan()
        {
            var genealogy = new FakeGenealogy()
                .WithLeader("clan_holder", "holder")
                .AddHero("holder", "clan_holder", childIds: new[] { "heir" })
                .AddHero("heir", "clan_holder");

            UseCase(genealogy, CountyA()).Execute();

            Assert.Empty(_claimRepository.AllClaims);
        }

        [Fact]
        public void IgnoresDeadDescendants()
        {
            var genealogy = new FakeGenealogy()
                .WithLeader("clan_holder", "holder")
                .AddHero("holder", "clan_holder", childIds: new[] { "son" })
                .AddHero("son", "clan_son", isAlive: false);

            UseCase(genealogy, CountyA()).Execute();

            Assert.Empty(_claimRepository.AllClaims);
        }

        [Fact]
        public void AnchorsTheClansDeadOnItsPrincipalTitleOnly()
        {
            var genealogy = new FakeGenealogy()
                .AddHero("ancestor", "clan_holder", isAlive: false, childIds: new[] { "descendant" })
                .AddHero("descendant", "clan_other")
                .AddHero("clan_holder", "clan_holder");

            UseCase(
                    genealogy,
                    new Title("duchy_x", "Duchy X", TitleRank.Duke, "seat_x", "clan_holder"),
                    new Title("barony_y", "Barony Y", TitleRank.Baron, "seat_y", "clan_holder"))
                .Execute();

            var claim = Assert.Single(_claimRepository.AllClaims);
            Assert.Equal("duchy_x", claim.TitleId);
            Assert.Equal(ClaimStrength.Strong, claim.Strength);
        }

        [Fact]
        public void RebuildsBloodClaimsButLeavesConquestClaimsAlone()
        {
            _claimRepository.AddClaim(
                new Claim("county_a:clan_old:conquest", "clan_old", "county_a", ClaimStrength.Strong,
                    ClaimOrigin.Conquest));
            _claimRepository.AddClaim(
                new Claim("county_a:ghost:blood", "clan_gone", "county_a", ClaimStrength.Strong,
                    ClaimOrigin.Inheritance, "ghost"));

            UseCase(ThreeGenerations(), CountyA()).Execute();

            Assert.Contains(_claimRepository.AllClaims, claim => claim.Id == "county_a:clan_old:conquest");
            Assert.DoesNotContain(_claimRepository.AllClaims, claim => claim.Id == "county_a:ghost:blood");
        }

        [Fact]
        public void IsIdempotent()
        {
            var useCase = UseCase(ThreeGenerations(), CountyA());

            var first = useCase.Execute();
            var second = useCase.Execute();

            Assert.Equal(first.Count, second.Count);
            Assert.Equal(first.Count, _claimRepository.AllClaims.Count);
        }

        [Fact]
        public void GrantsNothingOnAVacantTitle()
        {
            UseCase(ThreeGenerations(), CountyA(holderHeroId: null)).Execute();

            Assert.Empty(_claimRepository.AllClaims);
        }
    }
}
