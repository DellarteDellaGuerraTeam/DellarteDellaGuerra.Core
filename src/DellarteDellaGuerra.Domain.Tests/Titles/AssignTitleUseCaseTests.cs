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
        public void Execute_AssignsNewHolder_OnGrant()
        {
            var result = _useCase.Execute("settlement_a", "clan_new", SeatTransferKind.Grant, 10f);

            Assert.NotNull(result);
            Assert.Equal("county_a", result!.TitleId);
            Assert.Equal("clan_old", result.PreviousHolderClanId);
            Assert.Equal("clan_new", result.NewHolderClanId);
            Assert.False(result.Contested);
            Assert.Equal("clan_new", _titleRepository.GetTitle("county_a")!.HolderClanId);
        }

        [Fact]
        public void Execute_GeneratesConquestClaimForOustedHolder_OnGrant()
        {
            var result = _useCase.Execute("settlement_a", "clan_new", SeatTransferKind.Grant, 10f);

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
            var result = _useCase.Execute("settlement_unknown", "clan_new", SeatTransferKind.Grant, 10f);

            Assert.Null(result);
        }

        [Fact]
        public void Execute_DoesNotDuplicateClaim_OnRepeatTransfer()
        {
            _useCase.Execute("settlement_a", "clan_new", SeatTransferKind.Grant, 10f);
            _useCase.Execute("settlement_a", "clan_old", SeatTransferKind.Grant, 11f);
            var result = _useCase.Execute("settlement_a", "clan_new", SeatTransferKind.Grant, 12f);

            // clan_old was ousted again, but its conquest claim already exists.
            Assert.False(result!.ClaimGenerated);
            Assert.Single(
                _claimRepository.GetClaimsOn("county_a"),
                claim => claim.Id == "county_a:clan_old:conquest");
        }

        [Fact]
        public void Execute_GeneratesNoClaim_WhenSeatWasVacant()
        {
            var result = _useCase.Execute("settlement_b", "clan_new", SeatTransferKind.Grant, 10f);

            Assert.NotNull(result);
            Assert.False(result!.ClaimGenerated);
            Assert.Null(result.PreviousHolderClanId);
            Assert.Empty(_claimRepository.GetClaimsOn("county_b"));
        }

        [Fact]
        public void Execute_ReturnsNoChange_WhenHolderAlreadyOwnsSeat()
        {
            var result = _useCase.Execute("settlement_a", "clan_old", SeatTransferKind.Grant, 10f);

            Assert.NotNull(result);
            Assert.False(result!.ClaimGenerated);
            Assert.Equal("clan_old", _titleRepository.GetTitle("county_a")!.HolderClanId);
            Assert.Empty(_claimRepository.GetClaimsOn("county_a"));
        }

        [Fact]
        public void Execute_KeepsHolderAndMarksContested_OnConquest()
        {
            var result = _useCase.Execute("settlement_a", "clan_invader", SeatTransferKind.Conquest, 42f);

            Assert.NotNull(result);
            Assert.True(result!.Contested);
            Assert.False(result.ClaimGenerated);
            Assert.Equal("clan_old", result.NewHolderClanId);

            var title = _titleRepository.GetTitle("county_a")!;
            Assert.Equal("clan_old", title.HolderClanId);
            Assert.Equal("clan_invader", title.OccupantClanId);
            Assert.Equal(42f, title.ContestedSinceDay);
            Assert.True(title.IsContested);
            Assert.Empty(_claimRepository.GetClaimsOn("county_a"));
        }

        [Fact]
        public void Execute_ResolvesContest_WhenHolderRetakesSeat()
        {
            _useCase.Execute("settlement_a", "clan_invader", SeatTransferKind.Conquest, 42f);
            var result = _useCase.Execute("settlement_a", "clan_old", SeatTransferKind.Conquest, 50f);

            Assert.False(result!.Contested);
            var title = _titleRepository.GetTitle("county_a")!;
            Assert.Equal("clan_old", title.HolderClanId);
            Assert.Null(title.OccupantClanId);
            Assert.Null(title.ContestedSinceDay);
        }

        [Fact]
        public void Execute_KeepsContestedClock_OnRepeatConquestBySameOccupant()
        {
            _useCase.Execute("settlement_a", "clan_invader", SeatTransferKind.Conquest, 42f);
            var result = _useCase.Execute("settlement_a", "clan_invader", SeatTransferKind.Conquest, 60f);

            Assert.True(result!.Contested);
            Assert.Equal(42f, _titleRepository.GetTitle("county_a")!.ContestedSinceDay);
        }

        [Fact]
        public void Execute_RestartsContestedClock_WhenOccupantChanges()
        {
            _useCase.Execute("settlement_a", "clan_invader", SeatTransferKind.Conquest, 42f);
            _useCase.Execute("settlement_a", "clan_other_invader", SeatTransferKind.Conquest, 60f);

            var title = _titleRepository.GetTitle("county_a")!;
            Assert.Equal("clan_other_invader", title.OccupantClanId);
            Assert.Equal(60f, title.ContestedSinceDay);
        }

        [Fact]
        public void Execute_TransfersDirectly_OnConquestOfVacantTitle()
        {
            var result = _useCase.Execute("settlement_b", "clan_new", SeatTransferKind.Conquest, 42f);

            Assert.False(result!.Contested);
            var title = _titleRepository.GetTitle("county_b")!;
            Assert.Equal("clan_new", title.HolderClanId);
            Assert.Null(title.OccupantClanId);
        }

        [Fact]
        public void Execute_ClearsContestAndGeneratesClaim_OnGrantToOccupant()
        {
            _useCase.Execute("settlement_a", "clan_invader", SeatTransferKind.Conquest, 42f);
            var result = _useCase.Execute("settlement_a", "clan_invader", SeatTransferKind.Grant, 50f);

            Assert.False(result!.Contested);
            Assert.True(result.ClaimGenerated);
            var title = _titleRepository.GetTitle("county_a")!;
            Assert.Equal("clan_invader", title.HolderClanId);
            Assert.Null(title.OccupantClanId);
            Assert.Null(title.ContestedSinceDay);
            Assert.Single(
                _claimRepository.GetClaimsOn("county_a"),
                claim => claim.Id == "county_a:clan_old:conquest");
        }

        [Fact]
        public void Execute_ClearsContest_OnGrantBackToHolder()
        {
            _useCase.Execute("settlement_a", "clan_invader", SeatTransferKind.Conquest, 42f);
            var result = _useCase.Execute("settlement_a", "clan_old", SeatTransferKind.Grant, 50f);

            Assert.False(result!.Contested);
            Assert.False(result.ClaimGenerated);
            var title = _titleRepository.GetTitle("county_a")!;
            Assert.Equal("clan_old", title.HolderClanId);
            Assert.Null(title.OccupantClanId);
        }

        [Fact]
        public void Execute_TransfersDignity_OnAdministrativeChange()
        {
            _useCase.Execute("settlement_a", "clan_invader", SeatTransferKind.Conquest, 42f);
            var result = _useCase.Execute("settlement_a", "clan_heir", SeatTransferKind.Administrative, 50f);

            Assert.False(result!.Contested);
            var title = _titleRepository.GetTitle("county_a")!;
            Assert.Equal("clan_heir", title.HolderClanId);
            Assert.Null(title.OccupantClanId);
        }
    }
}