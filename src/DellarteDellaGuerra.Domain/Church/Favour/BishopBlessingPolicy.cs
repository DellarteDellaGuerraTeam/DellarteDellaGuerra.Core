namespace DellarteDellaGuerra.Domain.Church.Favour
{
    public enum BishopBlessingOutcome
    {
        Allowed,
        NotFavoured,
        OnCooldown
    }

    public static class BishopBlessingPolicy
    {
        public const int CooldownInDays = 7;
        public const int RenownGain = 1;

        /// <param name="rank">The Church's current favour rank for the player.</param>
        /// <param name="daysSinceLastBlessing">Days since the last bishop's blessing, or null if the player was never blessed.</param>
        public static BishopBlessingOutcome Evaluate(ChurchFavourRank rank, float? daysSinceLastBlessing)
        {
            if (rank < ChurchFavourRank.Favoured) return BishopBlessingOutcome.NotFavoured;
            if (daysSinceLastBlessing.HasValue && daysSinceLastBlessing.Value < CooldownInDays)
                return BishopBlessingOutcome.OnCooldown;
            return BishopBlessingOutcome.Allowed;
        }
    }
}
