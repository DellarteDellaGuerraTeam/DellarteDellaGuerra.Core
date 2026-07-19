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
