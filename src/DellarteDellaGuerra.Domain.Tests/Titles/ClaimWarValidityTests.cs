using DellarteDellaGuerra.Domain.Titles;

namespace DellarteDellaGuerra.Domain.Tests.Titles
{
    public class ClaimWarValidityTests
    {
        [Fact]
        public void IsVoid_DefenderStillHoldsTheTitle_False()
        {
            Assert.False(ClaimWarValidity.IsVoid("clan_defender", "clan_defender"));
        }

        [Fact]
        public void IsVoid_TitlePassedToAnotherHouse_True()
        {
            Assert.True(ClaimWarValidity.IsVoid("clan_defender", "clan_other"));
        }

        [Fact]
        public void IsVoid_TitleVacant_True()
        {
            Assert.True(ClaimWarValidity.IsVoid("clan_defender", null));
        }
    }
}
