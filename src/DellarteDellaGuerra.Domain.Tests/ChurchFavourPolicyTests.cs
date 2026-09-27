using DellarteDellaGuerra.Domain.Church.Favour;
using System.Collections.Generic;
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

        [Fact]
        public void GetProgress_AveragesClergyRelationsAndReturnsNextBoundaryRule()
        {
            var progress = ChurchFavourPolicy.GetProgress(new List<int> { -12, -8 });

            Assert.Equal(-10f, progress.AverageRelation);
            Assert.Equal(ChurchFavourRank.Reviled, progress.Rank);
            Assert.Equal(-10f, progress.NextBoundary);
            Assert.Equal(ChurchFavourBoundaryComparison.GreaterThan, progress.NextBoundaryComparison);
        }

        [Fact]
        public void GetProgress_WhenNoClergyDefaultsToIndifferentAtZero()
        {
            var progress = ChurchFavourPolicy.GetProgress(new List<int>());

            Assert.Equal(0f, progress.AverageRelation);
            Assert.Equal(ChurchFavourRank.Indifferent, progress.Rank);
            Assert.Equal(2f, progress.NextBoundary);
            Assert.Equal(ChurchFavourBoundaryComparison.AtLeast, progress.NextBoundaryComparison);
        }
    }
}
