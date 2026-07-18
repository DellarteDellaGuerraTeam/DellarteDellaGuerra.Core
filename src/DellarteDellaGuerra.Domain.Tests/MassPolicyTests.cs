using DellarteDellaGuerra.Domain.Church.Mass;
using Xunit;

namespace DellarteDellaGuerra.Domain.Tests.Church.Mass
{
    public class MassPolicyTests
    {
        [Fact]
        public void Evaluate_WhenNotSunday_ReturnsNotSunday()
        {
            var outcome = MassPolicy.Evaluate(isSunday: false, attendedToday: false);

            Assert.Equal(MassOutcome.NotSunday, outcome);
        }

        [Fact]
        public void Evaluate_WhenSundayAndNotYetAttended_ReturnsAllowed()
        {
            var outcome = MassPolicy.Evaluate(isSunday: true, attendedToday: false);

            Assert.Equal(MassOutcome.Allowed, outcome);
        }

        [Fact]
        public void Evaluate_WhenSundayAndAlreadyAttended_ReturnsAlreadyAttended()
        {
            var outcome = MassPolicy.Evaluate(isSunday: true, attendedToday: true);

            Assert.Equal(MassOutcome.AlreadyAttended, outcome);
        }
    }
}
