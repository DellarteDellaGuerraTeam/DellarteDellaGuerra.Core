namespace DellarteDellaGuerra.Domain.Church.Sanctuary
{
    public enum SanctuaryOutcome
    {
        Active,
        Expired
    }

    public static class SanctuaryPolicy
    {
        public const int PlayerSanctuaryDays = 40;
        public const int FugitiveSanctuaryDays = 20;

        /// <param name="daysElapsed">Days elapsed since sanctuary was claimed.</param>
        /// <param name="capInDays">The sanctuary term in days.</param>
        public static SanctuaryOutcome Evaluate(float daysElapsed, float capInDays)
        {
            return daysElapsed >= capInDays ? SanctuaryOutcome.Expired : SanctuaryOutcome.Active;
        }
    }
}
