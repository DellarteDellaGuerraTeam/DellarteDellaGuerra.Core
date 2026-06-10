using DellarteDellaGuerra.Domain.Levy;
using DellarteDellaGuerra.Domain.Levy.Model;

namespace DellarteDellaGuerra.Domain.Tests.Levy
{
    public class IssueLevyUseCaseTests
    {
        [Fact]
        public void Execute_IssuesCallForEachVassal()
        {
            var levyRepository = new FakeLevyRepository();
            var useCase = new IssueLevyUseCase(levyRepository);

            var issued = useCase.Execute("clan_lord", new[] { "clan_a", "clan_b", "clan_c" }, 10f);

            Assert.Equal(3, issued.Count);
            Assert.Equal(
                new[] { "clan_a", "clan_b", "clan_c" },
                issued.Select(call => call.VassalClanId).ToArray());
            Assert.Equal(3, levyRepository.GetAllLevyCalls().Count);
        }

        [Fact]
        public void Execute_SkipsVassalsWithExistingActiveCallFromSameLord()
        {
            var levyRepository = new FakeLevyRepository(
                new LevyCall("levy_existing", "clan_lord", "clan_a", 5f, LevyStatus.Called));
            var useCase = new IssueLevyUseCase(levyRepository);

            var issued = useCase.Execute("clan_lord", new[] { "clan_a", "clan_b" }, 10f);

            Assert.Single(issued);
            Assert.Equal("clan_b", issued[0].VassalClanId);
            Assert.Equal(2, levyRepository.GetAllLevyCalls().Count);
        }

        [Fact]
        public void Execute_IssuesCall_WhenExistingCallFromSameLordIsNotActive()
        {
            var levyRepository = new FakeLevyRepository(
                new LevyCall("levy_answered", "clan_lord", "clan_a", 5f, LevyStatus.Answered),
                new LevyCall("levy_refused", "clan_lord", "clan_b", 5f, LevyStatus.Refused));
            var useCase = new IssueLevyUseCase(levyRepository);

            var issued = useCase.Execute("clan_lord", new[] { "clan_a", "clan_b" }, 10f);

            Assert.Equal(2, issued.Count);
        }

        [Fact]
        public void Execute_IssuesCall_WhenActiveCallComesFromAnotherLord()
        {
            var levyRepository = new FakeLevyRepository(
                new LevyCall("levy_other", "clan_other_lord", "clan_a", 5f, LevyStatus.Called));
            var useCase = new IssueLevyUseCase(levyRepository);

            var issued = useCase.Execute("clan_lord", new[] { "clan_a" }, 10f);

            Assert.Single(issued);
            Assert.Equal("clan_lord", issued[0].IssuingClanId);
        }

        [Fact]
        public void Execute_GeneratedCallsHaveCalledStatusAndCorrectFields()
        {
            var levyRepository = new FakeLevyRepository();
            var useCase = new IssueLevyUseCase(levyRepository);

            var issued = useCase.Execute("clan_lord", new[] { "clan_a" }, 42.5f);

            var call = Assert.Single(issued);
            Assert.Equal(LevyStatus.Called, call.Status);
            Assert.Equal("clan_lord", call.IssuingClanId);
            Assert.Equal("clan_a", call.VassalClanId);
            Assert.Equal(42.5f, call.IssuedAtDay);
            Assert.False(string.IsNullOrEmpty(call.Id));
        }

        [Fact]
        public void Execute_SavesIssuedCallsInRepository()
        {
            var levyRepository = new FakeLevyRepository();
            var useCase = new IssueLevyUseCase(levyRepository);

            var issued = useCase.Execute("clan_lord", new[] { "clan_a", "clan_b" }, 10f);

            foreach (var call in issued)
            {
                Assert.Equal(call, levyRepository.GetLevyCall(call.Id));
            }
        }

        [Fact]
        public void Execute_GeneratesUniqueIds()
        {
            var levyRepository = new FakeLevyRepository();
            var useCase = new IssueLevyUseCase(levyRepository);

            var firstBatch = useCase.Execute("clan_lord", new[] { "clan_a" }, 10f);
            var answered = firstBatch[0] with { Status = LevyStatus.Answered };
            levyRepository.SaveLevyCall(answered);
            var secondBatch = useCase.Execute("clan_lord", new[] { "clan_a" }, 20f);

            Assert.NotEqual(firstBatch[0].Id, secondBatch[0].Id);
        }
    }
}
