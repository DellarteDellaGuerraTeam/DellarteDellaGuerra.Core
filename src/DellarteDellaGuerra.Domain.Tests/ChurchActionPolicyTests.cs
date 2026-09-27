using DellarteDellaGuerra.Domain.Church.Donation;
using DellarteDellaGuerra.Domain.Church.Favour;
using DellarteDellaGuerra.Domain.Church.Mass;
using DellarteDellaGuerra.Domain.Church.Sanctuary;
using Xunit;

namespace DellarteDellaGuerra.Domain.Tests.Church
{
    public class ChurchActionPolicyTests
    {
        [Theory]
        [InlineData(false, false, MassOutcome.NotSunday)]
        [InlineData(false, true, MassOutcome.NotSunday)]
        [InlineData(true, true, MassOutcome.AlreadyAttended)]
        [InlineData(true, false, MassOutcome.Allowed)]
        public void MassPolicy_EvaluatesSundayAndAttendance(
            bool isSunday,
            bool attendedToday,
            MassOutcome expected)
        {
            Assert.Equal(expected, MassPolicy.Evaluate(isSunday, attendedToday));
        }

        [Theory]
        [InlineData(39.999f, 40f, SanctuaryOutcome.Active)]
        [InlineData(40f, 40f, SanctuaryOutcome.Expired)]
        [InlineData(40.001f, 40f, SanctuaryOutcome.Expired)]
        [InlineData(-1f, 40f, SanctuaryOutcome.Active)]
        [InlineData(0f, 0f, SanctuaryOutcome.Expired)]
        public void SanctuaryPolicy_ExpiresAtTheInclusiveCap(
            float daysElapsed,
            float capInDays,
            SanctuaryOutcome expected)
        {
            Assert.Equal(expected, SanctuaryPolicy.Evaluate(daysElapsed, capInDays));
        }

        [Theory]
        [InlineData(99, null, 100, DonationOutcome.InsufficientGold)]
        [InlineData(99, 7f, 100, DonationOutcome.InsufficientGold)]
        [InlineData(100, null, 100, DonationOutcome.Allowed)]
        [InlineData(100, 7f, 100, DonationOutcome.Allowed)]
        [InlineData(100, 6.999f, 100, DonationOutcome.OnCooldown)]
        [InlineData(100, 0f, 100, DonationOutcome.OnCooldown)]
        [InlineData(100, -1f, 100, DonationOutcome.OnCooldown)]
        [InlineData(0, null, 0, DonationOutcome.Allowed)]
        [InlineData(-1, null, 0, DonationOutcome.InsufficientGold)]
        public void DonationPolicy_EvaluatesFundsBeforeTheSevenDayCooldown(
            int playerGold,
            float? daysSinceLastDonation,
            int costInGold,
            DonationOutcome expected)
        {
            Assert.Equal(
                expected,
                DonationPolicy.Evaluate(playerGold, daysSinceLastDonation, costInGold));
        }

        [Theory]
        [InlineData(ChurchFavourRank.Reviled, null, BishopBlessingOutcome.NotFavoured)]
        [InlineData(ChurchFavourRank.IllRegarded, 7f, BishopBlessingOutcome.NotFavoured)]
        [InlineData(ChurchFavourRank.Indifferent, 0f, BishopBlessingOutcome.NotFavoured)]
        [InlineData(ChurchFavourRank.Favoured, null, BishopBlessingOutcome.Allowed)]
        [InlineData(ChurchFavourRank.Favoured, 7f, BishopBlessingOutcome.Allowed)]
        [InlineData(ChurchFavourRank.Beloved, 7f, BishopBlessingOutcome.Allowed)]
        [InlineData(ChurchFavourRank.Favoured, 6.999f, BishopBlessingOutcome.OnCooldown)]
        [InlineData(ChurchFavourRank.Beloved, 0f, BishopBlessingOutcome.OnCooldown)]
        [InlineData(ChurchFavourRank.Beloved, -1f, BishopBlessingOutcome.OnCooldown)]
        public void BishopBlessingPolicy_RequiresFavourBeforeTheSevenDayCooldown(
            ChurchFavourRank rank,
            float? daysSinceLastBlessing,
            BishopBlessingOutcome expected)
        {
            Assert.Equal(
                expected,
                BishopBlessingPolicy.Evaluate(rank, daysSinceLastBlessing));
        }
    }
}
