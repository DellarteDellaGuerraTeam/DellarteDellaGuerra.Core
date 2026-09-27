using DellarteDellaGuerra.Domain.Church;
using Xunit;

namespace DellarteDellaGuerra.Domain.Tests.Church
{
    public class ChurchSettingsTests
    {
        [Fact]
        public void Constructor_PreservesEveryConfiguredValueWithoutValidationOrNormalization()
        {
            var settings = new ChurchSettings(
                donationCost: 0,
                donationRelation: -1,
                massRelation: 2,
                massMorale: -3,
                sacrilegeRelationLocal: 4,
                sacrilegeRelationOthers: -5,
                weeklyTithePower: 6,
                donationPower: -7,
                blessingMorale: 8,
                maxPilgrimParties: -9,
                pilgrimProtectionRelation: 10);

            Assert.Equal(0, settings.DonationCost);
            Assert.Equal(-1, settings.DonationRelation);
            Assert.Equal(2, settings.MassRelation);
            Assert.Equal(-3, settings.MassMorale);
            Assert.Equal(4, settings.SacrilegeRelationLocal);
            Assert.Equal(-5, settings.SacrilegeRelationOthers);
            Assert.Equal(6, settings.WeeklyTithePower);
            Assert.Equal(-7, settings.DonationPower);
            Assert.Equal(8, settings.BlessingMorale);
            Assert.Equal(-9, settings.MaxPilgrimParties);
            Assert.Equal(10, settings.PilgrimProtectionRelation);
        }
    }
}
