using DellarteDellaGuerra.Domain.Church.Donation;
using Xunit;

namespace DellarteDellaGuerra.Domain.Tests.Church.Donation
{
    public class DonationPolicyTests
    {
        private const int CostInGold = 500;

        [Fact]
        public void Evaluate_WhenGoldSufficientAndCooldownElapsed_ReturnsAllowed()
        {
            var outcome = DonationPolicy.Evaluate(CostInGold, 8f, CostInGold);

            Assert.Equal(DonationOutcome.Allowed, outcome);
        }

        [Fact]
        public void Evaluate_WhenNeverDonated_ReturnsAllowed()
        {
            var outcome = DonationPolicy.Evaluate(CostInGold, null, CostInGold);

            Assert.Equal(DonationOutcome.Allowed, outcome);
        }

        [Fact]
        public void Evaluate_WhenInsufficientGold_ReturnsInsufficientGold()
        {
            var outcome = DonationPolicy.Evaluate(CostInGold - 1, null, CostInGold);

            Assert.Equal(DonationOutcome.InsufficientGold, outcome);
        }

        [Fact]
        public void Evaluate_WhenOnCooldown_ReturnsOnCooldown()
        {
            var outcome = DonationPolicy.Evaluate(CostInGold, DonationPolicy.CooldownInDays - 0.1f, CostInGold);

            Assert.Equal(DonationOutcome.OnCooldown, outcome);
        }

        [Fact]
        public void Evaluate_WhenCooldownExactlyElapsed_ReturnsAllowed()
        {
            var outcome = DonationPolicy.Evaluate(CostInGold, DonationPolicy.CooldownInDays, CostInGold);

            Assert.Equal(DonationOutcome.Allowed, outcome);
        }
    }
}
