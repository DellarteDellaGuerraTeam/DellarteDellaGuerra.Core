using DellarteDellaGuerra.Domain.Church.Favour;
using Xunit;

namespace DellarteDellaGuerra.Domain.Tests.Church.Favour
{
    public class ChurchFavourPolicyTests
    {
        [Fact]
        public void Evaluate_WhenAverageIsExactlyMinusTen_ReturnsReviled()
        {
            Assert.Equal(ChurchFavourRank.Reviled, ChurchFavourPolicy.Evaluate(-10f));
        }

        [Fact]
        public void Evaluate_WhenAverageIsBelowMinusTen_ReturnsReviled()
        {
            Assert.Equal(ChurchFavourRank.Reviled, ChurchFavourPolicy.Evaluate(-25f));
        }

        [Fact]
        public void Evaluate_WhenAverageIsJustAboveMinusTen_ReturnsIllRegarded()
        {
            Assert.Equal(ChurchFavourRank.IllRegarded, ChurchFavourPolicy.Evaluate(-9.9f));
        }

        [Fact]
        public void Evaluate_WhenAverageIsExactlyMinusTwo_ReturnsIllRegarded()
        {
            Assert.Equal(ChurchFavourRank.IllRegarded, ChurchFavourPolicy.Evaluate(-2f));
        }

        [Fact]
        public void Evaluate_WhenAverageIsJustAboveMinusTwo_ReturnsIndifferent()
        {
            Assert.Equal(ChurchFavourRank.Indifferent, ChurchFavourPolicy.Evaluate(-1.9f));
        }

        [Fact]
        public void Evaluate_WhenAverageIsZero_ReturnsIndifferent()
        {
            Assert.Equal(ChurchFavourRank.Indifferent, ChurchFavourPolicy.Evaluate(0f));
        }

        [Fact]
        public void Evaluate_WhenAverageIsJustBelowPlusTwo_ReturnsIndifferent()
        {
            Assert.Equal(ChurchFavourRank.Indifferent, ChurchFavourPolicy.Evaluate(1.9f));
        }

        [Fact]
        public void Evaluate_WhenAverageIsExactlyPlusTwo_ReturnsFavoured()
        {
            Assert.Equal(ChurchFavourRank.Favoured, ChurchFavourPolicy.Evaluate(2f));
        }

        [Fact]
        public void Evaluate_WhenAverageIsJustBelowPlusTen_ReturnsFavoured()
        {
            Assert.Equal(ChurchFavourRank.Favoured, ChurchFavourPolicy.Evaluate(9.9f));
        }

        [Fact]
        public void Evaluate_WhenAverageIsExactlyPlusTen_ReturnsBeloved()
        {
            Assert.Equal(ChurchFavourRank.Beloved, ChurchFavourPolicy.Evaluate(10f));
        }
    }
}
