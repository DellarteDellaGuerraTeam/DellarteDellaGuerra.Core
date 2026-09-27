using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Church.Favour;
using Xunit;

namespace DellarteDellaGuerra.Domain.Tests.Church.Favour
{
    public class ChurchFavourProgressTests
    {
        public static IEnumerable<object[]> ProgressCases()
        {
            yield return new object[]
            {
                new[] { -10 },
                -10f,
                ChurchFavourRank.Reviled,
                -10f,
                ChurchFavourBoundaryComparison.GreaterThan
            };
            yield return new object[]
            {
                new[] { -2 },
                -2f,
                ChurchFavourRank.IllRegarded,
                -2f,
                ChurchFavourBoundaryComparison.GreaterThan
            };
            yield return new object[]
            {
                new[] { -1, 0 },
                -0.5f,
                ChurchFavourRank.Indifferent,
                2f,
                ChurchFavourBoundaryComparison.AtLeast
            };
            yield return new object[]
            {
                new[] { 2 },
                2f,
                ChurchFavourRank.Favoured,
                10f,
                ChurchFavourBoundaryComparison.AtLeast
            };
        }

        [Theory]
        [MemberData(nameof(ProgressCases))]
        public void GetProgress_ReturnsAverageRankAndNextBoundary(
            IReadOnlyCollection<int> relations,
            float expectedAverage,
            ChurchFavourRank expectedRank,
            float expectedBoundary,
            ChurchFavourBoundaryComparison expectedComparison)
        {
            var progress = ChurchFavourPolicy.GetProgress(relations);

            Assert.Equal(expectedAverage, progress.AverageRelation);
            Assert.Equal(expectedRank, progress.Rank);
            Assert.Equal(expectedBoundary, progress.NextBoundary);
            Assert.Equal(expectedComparison, progress.NextBoundaryComparison);
        }

        [Fact]
        public void GetProgress_WhenBeloved_HasNoFurtherBoundary()
        {
            var progress = ChurchFavourPolicy.GetProgress(new[] { 10 });

            Assert.Equal(10f, progress.AverageRelation);
            Assert.Equal(ChurchFavourRank.Beloved, progress.Rank);
            Assert.Null(progress.NextBoundary);
            Assert.Null(progress.NextBoundaryComparison);
        }
    }
}
