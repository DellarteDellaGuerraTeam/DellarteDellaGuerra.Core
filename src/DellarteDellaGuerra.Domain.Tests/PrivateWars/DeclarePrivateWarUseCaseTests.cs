using System.Collections.Generic;
using DellarteDellaGuerra.Domain.PrivateWars;
using DellarteDellaGuerra.Domain.PrivateWars.Model;

namespace DellarteDellaGuerra.Domain.Tests.PrivateWars
{
    public class DeclarePrivateWarUseCaseTests
    {
        private static readonly IReadOnlyDictionary<string, string> EmptySnapshot = new Dictionary<string, string>();

        [Fact]
        public void Execute_CreatesAndPersistsActiveWar()
        {
            var repository = new FakePrivateWarRepository();
            var useCase = new DeclarePrivateWarUseCase(repository);

            var war = useCase.Execute(
                "clan_attacker", "clan_defender", new ClaimCasusBelli("title_county"),
                mainGoalSettlementId: "settlement_goal", EmptySnapshot, startDay: 5f);

            Assert.NotNull(war);
            Assert.Equal(PrivateWarStatus.Active, war!.Status);
            Assert.Equal("claim", war.CasusBelliType);
            Assert.Equal("title_county", war.TitleId);
            Assert.Equal("settlement_goal", war.MainGoalSettlementId);
            Assert.Equal(0f, war.BattleScore);
            Assert.Single(repository.GetAll());
        }

        [Fact]
        public void Execute_LandlessDefendant_RejectedWithoutPersisting()
        {
            var repository = new FakePrivateWarRepository();
            var useCase = new DeclarePrivateWarUseCase(repository);

            var war = useCase.Execute(
                "clan_attacker", "clan_defender", new ClaimCasusBelli("title_county"),
                mainGoalSettlementId: null, EmptySnapshot, startDay: 5f);

            Assert.Null(war);
            Assert.Empty(repository.GetAll());
        }

        [Fact]
        public void Execute_DuplicatePrincipalPairSameCasusBelli_Rejected()
        {
            var repository = new FakePrivateWarRepository();
            var useCase = new DeclarePrivateWarUseCase(repository);
            useCase.Execute("A", "D", new ClaimCasusBelli("title_county"), "settlement_goal", EmptySnapshot, 0f);

            var second = useCase.Execute("A", "D", new ClaimCasusBelli("title_county"), "settlement_goal", EmptySnapshot, 1f);

            Assert.Null(second);
            Assert.Single(repository.GetAll());
        }

        [Fact]
        public void Execute_SamePairDifferentTitle_Allowed()
        {
            var repository = new FakePrivateWarRepository();
            var useCase = new DeclarePrivateWarUseCase(repository);
            useCase.Execute("A", "D", new ClaimCasusBelli("title_county"), "settlement_goal", EmptySnapshot, 0f);

            var second = useCase.Execute("A", "D", new ClaimCasusBelli("title_duchy"), "settlement_goal2", EmptySnapshot, 1f);

            Assert.NotNull(second);
            Assert.Equal(2, repository.GetAll().Count);
        }

        [Fact]
        public void Execute_DifferentOpponent_Allowed()
        {
            var repository = new FakePrivateWarRepository();
            var useCase = new DeclarePrivateWarUseCase(repository);
            useCase.Execute("A", "D", new ClaimCasusBelli("title_county"), "settlement_goal", EmptySnapshot, 0f);

            var second = useCase.Execute("A", "E", new ClaimCasusBelli("title_county"), "settlement_goal", EmptySnapshot, 1f);

            Assert.NotNull(second);
            Assert.Equal(2, repository.GetAll().Count);
        }
    }
}
