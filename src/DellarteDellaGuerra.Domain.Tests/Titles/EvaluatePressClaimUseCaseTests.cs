using DellarteDellaGuerra.Domain.Titles;
using DellarteDellaGuerra.Domain.Titles.Model;

namespace DellarteDellaGuerra.Domain.Tests.Titles
{
    public class EvaluatePressClaimUseCaseTests
    {
        private static ClaimOpportunity Opportunity(
            ClaimStrength strength = ClaimStrength.Weak,
            float attackerStrength = 100f,
            float defenderStrength = 80f,
            bool defenderDistracted = false,
            float relation = 0f) =>
            new(
                "title",
                "attacker_clan",
                "defender_clan",
                strength,
                attackerStrength,
                defenderStrength,
                defenderDistracted,
                relation);

        private static readonly EvaluatePressClaimUseCase UseCase = new();

        [Fact]
        public void Execute_ReturnsZero_WhenStrengthRatioIsBelowTheFloor()
        {
            var opportunity = Opportunity(strength: ClaimStrength.DeJure, attackerStrength: 100f, defenderStrength: 100f);

            var score = UseCase.Execute(opportunity);

            Assert.Equal(0f, score);
        }

        [Fact]
        public void Execute_ScoresDeJureAboveStrongAboveWeak_AtAnIdenticalRatio()
        {
            var weak = UseCase.Execute(Opportunity(strength: ClaimStrength.Weak));
            var strong = UseCase.Execute(Opportunity(strength: ClaimStrength.Strong));
            var deJure = UseCase.Execute(Opportunity(strength: ClaimStrength.DeJure));

            Assert.True(deJure > strong);
            Assert.True(strong > weak);
        }

        [Fact]
        public void Execute_ReturnsHigherScore_WhenTheDefenderIsDistracted()
        {
            var notDistracted = UseCase.Execute(Opportunity(defenderDistracted: false));
            var distracted = UseCase.Execute(Opportunity(defenderDistracted: true));

            Assert.True(distracted > notDistracted);
        }

        [Fact]
        public void Execute_ReturnsHigherScore_WhenTheClanLeadersHateEachOther()
        {
            var neutral = UseCase.Execute(Opportunity(relation: 0f));
            var hatred = UseCase.Execute(Opportunity(relation: -100f));

            Assert.True(hatred > neutral);
        }

        [Fact]
        public void Execute_ReturnsLowerScore_WhenTheClanLeadersAreFriends()
        {
            var neutral = UseCase.Execute(Opportunity(relation: 0f));
            var friendship = UseCase.Execute(Opportunity(relation: 100f));

            Assert.True(friendship < neutral);
        }

        [Fact]
        public void Execute_DoesNotDivideByZero_AndScoresAtLeastAsHighAsAFiniteRatio()
        {
            var zeroDefenderStrength = UseCase.Execute(Opportunity(attackerStrength: 100f, defenderStrength: 0f));
            var finiteRatio = UseCase.Execute(Opportunity(attackerStrength: 100f, defenderStrength: 50f));

            Assert.True(zeroDefenderStrength >= finiteRatio);
        }

        [Fact]
        public void Execute_NeverReturnsBelowZero_ForAWeakClaimWithMaximumFriendship()
        {
            var opportunity = Opportunity(strength: ClaimStrength.Weak, relation: 100f);

            var score = UseCase.Execute(opportunity);

            Assert.True(score >= 0f);
        }
    }
}
