using DellarteDellaGuerra.Domain.Levy;
using DellarteDellaGuerra.Domain.Levy.Model;

namespace DellarteDellaGuerra.Domain.Tests.Levy
{
    public class ExpireLeviesUseCaseTests
    {
        private const float ExpiryDays = 7f;

        [Fact]
        public void Execute_RefusesCalledCallsOlderThanExpiryDays()
        {
            var levyRepository = new FakeLevyRepository(
                new LevyCall("levy_old", "clan_lord", "clan_a", 1f, LevyStatus.Called));
            var useCase = new ExpireLeviesUseCase(levyRepository);

            var refused = useCase.Execute(10f, ExpiryDays);

            var call = Assert.Single(refused);
            Assert.Equal("levy_old", call.Id);
            Assert.Equal(LevyStatus.Refused, call.Status);
        }

        [Fact]
        public void Execute_LeavesRecentCalledCallsUntouched()
        {
            var levyRepository = new FakeLevyRepository(
                new LevyCall("levy_recent", "clan_lord", "clan_a", 8f, LevyStatus.Called));
            var useCase = new ExpireLeviesUseCase(levyRepository);

            var refused = useCase.Execute(10f, ExpiryDays);

            Assert.Empty(refused);
            Assert.Equal(LevyStatus.Called, levyRepository.GetLevyCall("levy_recent")!.Status);
        }

        [Fact]
        public void Execute_LeavesAnsweredCallsUntouched()
        {
            var levyRepository = new FakeLevyRepository(
                new LevyCall("levy_answered", "clan_lord", "clan_a", 1f, LevyStatus.Answered));
            var useCase = new ExpireLeviesUseCase(levyRepository);

            var refused = useCase.Execute(10f, ExpiryDays);

            Assert.Empty(refused);
            Assert.Equal(LevyStatus.Answered, levyRepository.GetLevyCall("levy_answered")!.Status);
        }

        [Fact]
        public void Execute_LeavesAlreadyRefusedCallsUntouched()
        {
            var levyRepository = new FakeLevyRepository(
                new LevyCall("levy_refused", "clan_lord", "clan_a", 1f, LevyStatus.Refused));
            var useCase = new ExpireLeviesUseCase(levyRepository);

            var refused = useCase.Execute(10f, ExpiryDays);

            Assert.Empty(refused);
        }

        [Fact]
        public void Execute_ExpiresCallExactlyAtExpiryThreshold()
        {
            var levyRepository = new FakeLevyRepository(
                new LevyCall("levy_threshold", "clan_lord", "clan_a", 3f, LevyStatus.Called));
            var useCase = new ExpireLeviesUseCase(levyRepository);

            var refused = useCase.Execute(10f, ExpiryDays);

            Assert.Single(refused);
        }

        [Fact]
        public void Execute_UpdatesRepositoryWithRefusedStatus()
        {
            var levyRepository = new FakeLevyRepository(
                new LevyCall("levy_old", "clan_lord", "clan_a", 1f, LevyStatus.Called));
            var useCase = new ExpireLeviesUseCase(levyRepository);

            useCase.Execute(10f, ExpiryDays);

            var saved = levyRepository.GetLevyCall("levy_old");
            Assert.NotNull(saved);
            Assert.Equal(LevyStatus.Refused, saved!.Status);
        }

        [Fact]
        public void Execute_ReturnsOnlyTheNewlyRefusedCalls()
        {
            var levyRepository = new FakeLevyRepository(
                new LevyCall("levy_old_a", "clan_lord", "clan_a", 1f, LevyStatus.Called),
                new LevyCall("levy_old_b", "clan_lord", "clan_b", 2f, LevyStatus.Called),
                new LevyCall("levy_recent", "clan_lord", "clan_c", 9f, LevyStatus.Called),
                new LevyCall("levy_answered", "clan_lord", "clan_d", 1f, LevyStatus.Answered),
                new LevyCall("levy_refused", "clan_lord", "clan_e", 1f, LevyStatus.Refused));
            var useCase = new ExpireLeviesUseCase(levyRepository);

            var refused = useCase.Execute(10f, ExpiryDays);

            Assert.Equal(
                new[] { "levy_old_a", "levy_old_b" },
                refused.Select(call => call.Id).OrderBy(id => id).ToArray());
            Assert.All(refused, call => Assert.Equal(LevyStatus.Refused, call.Status));
        }
    }
}
