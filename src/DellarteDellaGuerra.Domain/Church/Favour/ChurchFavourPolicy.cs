using System.Collections.Generic;
using System.Linq;

namespace DellarteDellaGuerra.Domain.Church.Favour
{
    public enum ChurchFavourRank
    {
        Reviled,
        IllRegarded,
        Indifferent,
        Favoured,
        Beloved
    }

    public static class ChurchFavourPolicy
    {
        public static ChurchFavourProgress GetProgress(IReadOnlyCollection<int> clergyRelations)
        {
            var averageRelation = clergyRelations.Count == 0 ? 0f : (float)clergyRelations.Average();
            var rank = Evaluate(averageRelation);

            switch (rank)
            {
                case ChurchFavourRank.Reviled:
                    return new ChurchFavourProgress(
                        averageRelation, rank, -10f, ChurchFavourBoundaryComparison.GreaterThan);
                case ChurchFavourRank.IllRegarded:
                    return new ChurchFavourProgress(
                        averageRelation, rank, -2f, ChurchFavourBoundaryComparison.GreaterThan);
                case ChurchFavourRank.Indifferent:
                    return new ChurchFavourProgress(
                        averageRelation, rank, 2f, ChurchFavourBoundaryComparison.AtLeast);
                case ChurchFavourRank.Favoured:
                    return new ChurchFavourProgress(
                        averageRelation, rank, 10f, ChurchFavourBoundaryComparison.AtLeast);
                default:
                    return new ChurchFavourProgress(averageRelation, rank, null, null);
            }
        }

        /// <param name="averageRelation">The player's average relation with every living clergy notable, or 0 if none are alive.</param>
        public static ChurchFavourRank Evaluate(float averageRelation)
        {
            if (averageRelation <= -10f) return ChurchFavourRank.Reviled;
            if (averageRelation <= -2f) return ChurchFavourRank.IllRegarded;
            if (averageRelation < 2f) return ChurchFavourRank.Indifferent;
            if (averageRelation < 10f) return ChurchFavourRank.Favoured;
            return ChurchFavourRank.Beloved;
        }
    }
}
