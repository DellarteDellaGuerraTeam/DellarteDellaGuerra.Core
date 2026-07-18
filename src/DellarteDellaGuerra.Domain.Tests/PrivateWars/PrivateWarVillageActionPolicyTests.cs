using DellarteDellaGuerra.Domain.PrivateWars;

namespace DellarteDellaGuerra.Domain.Tests.PrivateWars
{
    public class PrivateWarVillageActionPolicyTests
    {
        private readonly PrivateWarVillageActionPolicy _policy = new();

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void EvaluateHostileAction_PreservesVanillaResultAndAvailability(bool vanillaEnabled)
        {
            var decision = _policy.EvaluateHostileAction(
                vanillaVisible: true,
                vanillaEnabled,
                isVillage: false,
                isNormalVillage: false,
                canLeadArmyAction: false,
                arePrivateEnemies: false);

            Assert.True(decision.IsVisible);
            Assert.Equal(vanillaEnabled, decision.IsEnabled);
        }

        [Fact]
        public void EvaluateHostileAction_PromotesNormalPrivateEnemyVillageForArmyLeader()
        {
            var decision = _policy.EvaluateHostileAction(
                vanillaVisible: false,
                vanillaEnabled: true,
                isVillage: true,
                isNormalVillage: true,
                canLeadArmyAction: true,
                arePrivateEnemies: true);

            Assert.True(decision.IsVisible);
            Assert.True(decision.IsEnabled);
        }

        [Theory]
        [InlineData(false, true, true, true)]
        [InlineData(true, false, true, true)]
        [InlineData(true, true, false, true)]
        [InlineData(true, true, true, false)]
        public void EvaluateHostileAction_RejectsInvalidVillageOrActionContext(
            bool isVillage,
            bool isNormalVillage,
            bool canLeadArmyAction,
            bool arePrivateEnemies)
        {
            var decision = _policy.EvaluateHostileAction(
                vanillaVisible: false,
                vanillaEnabled: true,
                isVillage,
                isNormalVillage,
                canLeadArmyAction,
                arePrivateEnemies);

            Assert.False(decision.IsVisible);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void EvaluateRaid_PreservesVanillaAvailabilityWhenPromotingPrivateEnemy(bool vanillaEnabled)
        {
            var decision = _policy.EvaluateRaid(
                vanillaVisible: false,
                vanillaEnabled,
                isVillage: true,
                arePrivateEnemies: true);

            Assert.True(decision.IsVisible);
            Assert.Equal(vanillaEnabled, decision.IsEnabled);
        }

        [Fact]
        public void EvaluateRaid_DoesNotPromoteOrdinaryOrNonVillageTargets()
        {
            Assert.False(_policy.EvaluateRaid(
                vanillaVisible: false,
                vanillaEnabled: true,
                isVillage: true,
                arePrivateEnemies: false).IsVisible);
            Assert.False(_policy.EvaluateRaid(
                vanillaVisible: false,
                vanillaEnabled: true,
                isVillage: false,
                arePrivateEnemies: true).IsVisible);
        }
    }
}
