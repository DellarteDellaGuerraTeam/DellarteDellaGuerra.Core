using DellarteDellaGuerra.Domain.Church.Favour;
using Xunit;

namespace DellarteDellaGuerra.Domain.Tests.Church.Favour
{
    public class BishopBlessingPolicyTests
    {
        [Fact]
        public void Evaluate_WhenFavouredAndNeverBlessed_ReturnsAllowed()
        {
            var outcome = BishopBlessingPolicy.Evaluate(ChurchFavourRank.Favoured, null);

            Assert.Equal(BishopBlessingOutcome.Allowed, outcome);
        }

        [Fact]
        public void Evaluate_WhenBelovedAndCooldownElapsed_ReturnsAllowed()
        {
            var outcome = BishopBlessingPolicy.Evaluate(ChurchFavourRank.Beloved, 8f);

            Assert.Equal(BishopBlessingOutcome.Allowed, outcome);
        }

        [Fact]
        public void Evaluate_WhenIndifferent_ReturnsNotFavoured()
        {
            var outcome = BishopBlessingPolicy.Evaluate(ChurchFavourRank.Indifferent, null);

            Assert.Equal(BishopBlessingOutcome.NotFavoured, outcome);
        }

        [Fact]
        public void Evaluate_WhenIllRegarded_ReturnsNotFavoured()
        {
            var outcome = BishopBlessingPolicy.Evaluate(ChurchFavourRank.IllRegarded, null);

            Assert.Equal(BishopBlessingOutcome.NotFavoured, outcome);
        }

        [Fact]
        public void Evaluate_WhenReviled_ReturnsNotFavoured()
        {
            var outcome = BishopBlessingPolicy.Evaluate(ChurchFavourRank.Reviled, null);

            Assert.Equal(BishopBlessingOutcome.NotFavoured, outcome);
        }

        [Fact]
        public void Evaluate_WhenOnCooldown_ReturnsOnCooldown()
        {
            var outcome = BishopBlessingPolicy.Evaluate(
                ChurchFavourRank.Favoured, BishopBlessingPolicy.CooldownInDays - 0.1f);

            Assert.Equal(BishopBlessingOutcome.OnCooldown, outcome);
        }

        [Fact]
        public void Evaluate_WhenCooldownExactlyElapsed_ReturnsAllowed()
        {
            var outcome = BishopBlessingPolicy.Evaluate(
                ChurchFavourRank.Favoured, BishopBlessingPolicy.CooldownInDays);

            Assert.Equal(BishopBlessingOutcome.Allowed, outcome);
        }
    }
}
