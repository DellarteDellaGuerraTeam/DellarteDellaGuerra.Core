using System.Collections.Generic;
using System.Linq;
using Bannerlord.PrivateWars.Domain;
using Bannerlord.PrivateWars.Domain.Model;

namespace DellarteDellaGuerra.Domain.Tests.PrivateWars
{
    public class DeclareWarUseCaseTests
    {
        private static readonly IReadOnlyDictionary<string, string> EmptySnapshot = new Dictionary<string, string>();
        private static readonly IReadOnlyCollection<string> NoSupporters = PrivateWarTestData.NoSupporters;

        [Fact]
        public void Execute_CreatesAndPersistsActiveWar()
        {
            var repository = new FakePrivateWarRepository();
            var useCase = new DeclarePrivateWarUseCase(repository, new FakePrivateWarSidesProvider());

            var war = useCase.Execute(
                "clan_attacker", "clan_defender", new ClaimCasusBelli("title_county"),
                mainGoalSettlementId: "settlement_goal", EmptySnapshot, startDay: 5f, NoSupporters, NoSupporters);

            Assert.NotNull(war);
            Assert.Equal(PrivateWarStatus.Active, war!.Status);
            Assert.Equal("claim", war.CasusBelliType);
            Assert.Equal("title_county", war.TitleId);
            Assert.Equal("settlement_goal", war.MainGoalSettlementId);
            Assert.Equal(0f, war.BattleScore);
            Assert.Single(repository.GetAll());
            Assert.Empty(war.ForeignSupporters);
        }

        [Fact]
        public void Execute_KeepsOnlyFiefsHeldByEitherSide()
        {
            var sides = new FakePrivateWarSidesProvider(new Dictionary<string, string> { ["V"] = "D" });
            var useCase = new DeclarePrivateWarUseCase(new FakePrivateWarRepository(), sides);
            var snapshot = new Dictionary<string, string>
            {
                ["town_a"] = "A",     // attacker principal
                ["castle_d"] = "D",   // defender principal
                ["castle_v"] = "V",   // defender's vassal
                ["town_u"] = "U"      // uninvolved clan
            };

            var war = useCase.Execute("A", "D", new ClaimCasusBelli("title_county"),
                "castle_d", snapshot, 0f, NoSupporters, NoSupporters);

            Assert.Equal(
                new[] { "castle_d", "castle_v", "town_a" },
                war!.OriginalFiefOwners.Keys.OrderBy(id => id));
        }

        [Fact]
        public void Execute_ForeignSupporters_AreCarriedOntoTheWar()
        {
            var useCase = new DeclarePrivateWarUseCase(new FakePrivateWarRepository(), new FakePrivateWarSidesProvider());

            var war = useCase.Execute(
                "clan_attacker", "clan_defender", new ClaimCasusBelli("title_county"),
                mainGoalSettlementId: "settlement_goal", EmptySnapshot, startDay: 5f,
                new[] { "clan_ally", "clan_foreign" }, NoSupporters, new[] { "clan_foreign" });

            Assert.Equal(new[] { "clan_foreign" }, war!.ForeignSupporters);
        }

        [Fact]
        public void Execute_LandlessDefendant_RejectedWithoutPersisting()
        {
            var repository = new FakePrivateWarRepository();
            var useCase = new DeclarePrivateWarUseCase(repository, new FakePrivateWarSidesProvider());

            var war = useCase.Execute(
                "clan_attacker", "clan_defender", new ClaimCasusBelli("title_county"),
                mainGoalSettlementId: null, EmptySnapshot, startDay: 5f, NoSupporters, NoSupporters);

            Assert.Null(war);
            Assert.Empty(repository.GetAll());
        }

        [Fact]
        public void Execute_DuplicatePrincipalPairSameCasusBelli_Rejected()
        {
            var repository = new FakePrivateWarRepository();
            var useCase = new DeclarePrivateWarUseCase(repository, new FakePrivateWarSidesProvider());
            useCase.Execute("A", "D", new ClaimCasusBelli("title_county"), "settlement_goal", EmptySnapshot, 0f, NoSupporters, NoSupporters);

            var second = useCase.Execute("A", "D", new ClaimCasusBelli("title_county"), "settlement_goal", EmptySnapshot, 1f, NoSupporters, NoSupporters);

            Assert.Null(second);
            Assert.Single(repository.GetAll());
        }

        [Fact]
        public void Execute_SamePairDifferentTitle_Allowed()
        {
            var repository = new FakePrivateWarRepository();
            var useCase = new DeclarePrivateWarUseCase(repository, new FakePrivateWarSidesProvider());
            useCase.Execute("A", "D", new ClaimCasusBelli("title_county"), "settlement_goal", EmptySnapshot, 0f, NoSupporters, NoSupporters);

            var second = useCase.Execute("A", "D", new ClaimCasusBelli("title_duchy"), "settlement_goal2", EmptySnapshot, 1f, NoSupporters, NoSupporters);

            Assert.NotNull(second);
            Assert.Equal(2, repository.GetAll().Count);
        }

        [Fact]
        public void Execute_DifferentOpponent_Allowed()
        {
            var repository = new FakePrivateWarRepository();
            var useCase = new DeclarePrivateWarUseCase(repository, new FakePrivateWarSidesProvider());
            useCase.Execute("A", "D", new ClaimCasusBelli("title_county"), "settlement_goal", EmptySnapshot, 0f, NoSupporters, NoSupporters);

            var second = useCase.Execute("A", "E", new ClaimCasusBelli("title_county"), "settlement_goal", EmptySnapshot, 1f, NoSupporters, NoSupporters);

            Assert.NotNull(second);
            Assert.Equal(2, repository.GetAll().Count);
        }
    }
}
