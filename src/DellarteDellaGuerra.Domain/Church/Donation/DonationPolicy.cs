namespace DellarteDellaGuerra.Domain.Church.Donation
{
    public enum DonationOutcome
    {
        Allowed,
        InsufficientGold,
        OnCooldown
    }

    public static class DonationPolicy
    {
        public const int CooldownInDays = 7;
        public const int RenownGain = 1;

        /// <param name="playerGold">The player's current gold.</param>
        /// <param name="daysSinceLastDonation">Days since the last donation to this abbot, or null if the player never donated.</param>
        /// <param name="costInGold">The gold cost of a donation.</param>
        public static DonationOutcome Evaluate(int playerGold, float? daysSinceLastDonation, int costInGold)
        {
            if (playerGold < costInGold) return DonationOutcome.InsufficientGold;
            if (daysSinceLastDonation.HasValue && daysSinceLastDonation.Value < CooldownInDays)
                return DonationOutcome.OnCooldown;
            return DonationOutcome.Allowed;
        }
    }
}
