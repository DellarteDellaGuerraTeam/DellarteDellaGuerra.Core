namespace DellarteDellaGuerra.Domain.Church.Mass
{
    public enum MassOutcome
    {
        Allowed,
        NotSunday,
        AlreadyAttended
    }

    public static class MassPolicy
    {
        public const int MoraleGain = 4;
        public const int RelationGain = 1;

        /// <param name="isSunday">Whether the current campaign day is Sunday.</param>
        /// <param name="attendedToday">Whether the player already attended mass today.</param>
        public static MassOutcome Evaluate(bool isSunday, bool attendedToday)
        {
            if (!isSunday) return MassOutcome.NotSunday;
            if (attendedToday) return MassOutcome.AlreadyAttended;
            return MassOutcome.Allowed;
        }
    }
}
