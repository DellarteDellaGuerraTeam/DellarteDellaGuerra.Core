namespace DellarteDellaGuerra.Domain.Church.Favour
{
    public enum ChurchFavourBoundaryComparison
    {
        GreaterThan,
        AtLeast
    }

    public class ChurchFavourProgress
    {
        public ChurchFavourProgress(
            float averageRelation,
            ChurchFavourRank rank,
            float? nextBoundary,
            ChurchFavourBoundaryComparison? nextBoundaryComparison)
        {
            AverageRelation = averageRelation;
            Rank = rank;
            NextBoundary = nextBoundary;
            NextBoundaryComparison = nextBoundaryComparison;
        }

        public float AverageRelation { get; }
        public ChurchFavourRank Rank { get; }
        public float? NextBoundary { get; }
        public ChurchFavourBoundaryComparison? NextBoundaryComparison { get; }
    }
}
