using DellarteDellaGuerra.Domain.Titles;
using DellarteDellaGuerra.Domain.Titles.Model;

namespace DellarteDellaGuerra.Domain.Tests.Titles
{
    public class AssignTitleUseCaseTests
    {
        private readonly FakeTitleRepository _titleRepository;
        private readonly FakeClaimRepository _claimRepository;
        private readonly AssignTitleUseCase _useCase;

        public AssignTitleUseCaseTests()
        {
            _titleRepository = new FakeTitleRepository(
                new Title("county_a", "County of A", TitleRank.Count, "settlement_a", "clan_old"),
                new Title("county_b", "County of B", TitleRank.Count, "settlement_b", null));
            _claimRepository = new FakeClaimRepository();
            _useCase = new AssignTitleUseCase(_titleRepository, _claimRepository, new FakeLoggerFactory());
        }

        [Fact]
        public void Execute_AssignsNewHolder()
        {
            var result = _useCase.Execute("settlement_a", "clan_new");

            Assert.NotNull(result);
            Assert.Equal("county_a", result!.TitleId);
            Assert.Equal("clan_old", result.PreviousHolderClanId);
            Assert.Equal("clan_new", result.NewHolderClanId);
            Assert.Equal("clan_new", _titleRepository.GetTitle("county_a")!.HolderClanId);
        }

        [Fact]
        public void Execute_GeneratesConquestClaimForOustedHolder()
        {
            var result = _useCase.Execute("settlement_a", "clan_new");

            Assert.True(result!.ClaimGenerated);
            var claim = Assert.Single(_claimRepository.GetClaimsOn("county_a"));
            Assert.Equal("county_a:clan_old:conquest", claim.Id);
            Assert.Equal("clan_old", claim.ClaimantClanId);
            Assert.Equal(ClaimStrength.Strong, claim.Strength);
            Assert.Equal(ClaimOrigin.Conquest, claim.Origin);
        }

        [Fact]
        public void Execute_ReturnsNull_WhenSettlementUnknown()
        {
            var result = _useCase.Execute("settlement_unknown", "clan_new");

            Assert.Null(result);
        }

        [Fact]
        public void Execute_DoesNotDuplicateClaim_OnRepeatConquest()
        {
            _useCase.Execute("settlement_a", "clan_new");
            _useCase.Execute("settlement_a", "clan_old");
            var result = _useCase.Execute("settlement_a", "clan_new");

            // clan_old was ousted again, but its conquest claim already exists.
            Assert.False(result!.ClaimGenerated);
            Assert.Single(
                _claimRepository.GetClaimsOn("county_a"),
                claim => claim.Id == "county_a:clan_old:conquest");
        }

        [Fact]
        public void Execute_GeneratesNoClaim_WhenSeatWasVacant()
        {
            var result = _useCase.Execute("settlement_b", "clan_new");

            Assert.NotNull(result);
            Assert.False(result!.ClaimGenerated);
            Assert.Null(result.PreviousHolderClanId);
            Assert.Empty(_claimRepository.GetClaimsOn("county_b"));
        }

        [Fact]
        public void Execute_ReturnsNoChange_WhenHolderAlreadyOwnsSeat()
        {
            var result = _useCase.Execute("settlement_a", "clan_old");

            Assert.NotNull(result);
            Assert.False(result!.ClaimGenerated);
            Assert.Equal("clan_old", _titleRepository.GetTitle("county_a")!.HolderClanId);
            Assert.Empty(_claimRepository.GetClaimsOn("county_a"));
        }
    }
}
