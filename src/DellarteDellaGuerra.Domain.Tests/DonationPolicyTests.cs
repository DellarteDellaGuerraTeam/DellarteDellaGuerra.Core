using DellarteDellaGuerra.Domain.Church.Donation;
using Xunit;

namespace DellarteDellaGuerra.Domain.Tests.Church.Donation
{
    public class DonationPolicyTests
    {
        [Fact]
        public void Evaluate_WhenGoldSufficientAndCooldownElapsed_ReturnsAllowed()
        {
            var outcome = DonationPolicy.Evaluate(DonationPolicy.CostInGold, 8f);

            Assert.Equal(DonationOutcome.Allowed, outcome);
        }

        [Fact]
        public void Evaluate_WhenNeverDonated_ReturnsAllowed()
        {
            var outcome = DonationPolicy.Evaluate(DonationPolicy.CostInGold, null);

            Assert.Equal(DonationOutcome.Allowed, outcome);
        }

        [Fact]
        public void Evaluate_WhenInsufficientGold_ReturnsInsufficientGold()
        {
            var outcome = DonationPolicy.Evaluate(DonationPolicy.CostInGold - 1, null);

            Assert.Equal(DonationOutcome.InsufficientGold, outcome);
        }

        [Fact]
        public void Evaluate_WhenOnCooldown_ReturnsOnCooldown()
        {
            var outcome = DonationPolicy.Evaluate(DonationPolicy.CostInGold, DonationPolicy.CooldownInDays - 0.1f);

            Assert.Equal(DonationOutcome.OnCooldown, outcome);
        }

        [Fact]
        public void Evaluate_WhenCooldownExactlyElapsed_ReturnsAllowed()
        {
            var outcome = DonationPolicy.Evaluate(DonationPolicy.CostInGold, DonationPolicy.CooldownInDays);

            Assert.Equal(DonationOutcome.Allowed, outcome);
        }
    }
}
