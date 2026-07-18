using DellarteDellaGuerra.Domain.Church.Sanctuary;
using Xunit;

namespace DellarteDellaGuerra.Domain.Tests.Church.Sanctuary
{
    public class SanctuaryPolicyTests
    {
        [Fact]
        public void Evaluate_WhenJustUnderCap_ReturnsActive()
        {
            var outcome = SanctuaryPolicy.Evaluate(
                SanctuaryPolicy.PlayerSanctuaryDays - 0.1f, SanctuaryPolicy.PlayerSanctuaryDays);

            Assert.Equal(SanctuaryOutcome.Active, outcome);
        }

        [Fact]
        public void Evaluate_WhenExactlyAtCap_ReturnsExpired()
        {
            var outcome = SanctuaryPolicy.Evaluate(
                SanctuaryPolicy.PlayerSanctuaryDays, SanctuaryPolicy.PlayerSanctuaryDays);

            Assert.Equal(SanctuaryOutcome.Expired, outcome);
        }

        [Fact]
        public void Evaluate_WhenOverCap_ReturnsExpired()
        {
            var outcome = SanctuaryPolicy.Evaluate(
                SanctuaryPolicy.FugitiveSanctuaryDays + 1f, SanctuaryPolicy.FugitiveSanctuaryDays);

            Assert.Equal(SanctuaryOutcome.Expired, outcome);
        }
    }
}
