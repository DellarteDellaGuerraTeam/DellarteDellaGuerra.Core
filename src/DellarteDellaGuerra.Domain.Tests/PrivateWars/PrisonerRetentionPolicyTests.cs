using Bannerlord.PrivateWars.Domain;

namespace DellarteDellaGuerra.Domain.Tests.PrivateWars
{
    public class PrisonerRetentionPolicyTests
    {
        private readonly PrivateWarPrisonerRetentionPolicy _policy = new();

        [Theory]
        [InlineData(PrivateWarPrisonerReleaseReason.AfterPeace)]
        [InlineData(PrivateWarPrisonerReleaseReason.AfterBattle)]
        public void ShouldAllowRelease_BlocksInvoluntaryReleaseForPrivateEnemies(
            PrivateWarPrisonerReleaseReason reason)
        {
            Assert.False(_policy.ShouldAllowRelease(
                isMainHero: false,
                reason,
                arePrivateEnemies: true));
        }

        [Theory]
        [InlineData(PrivateWarPrisonerReleaseReason.Ransom)]
        [InlineData(PrivateWarPrisonerReleaseReason.Escape)]
        [InlineData(PrivateWarPrisonerReleaseReason.DeliberateRelease)]
        [InlineData(PrivateWarPrisonerReleaseReason.Death)]
        [InlineData(PrivateWarPrisonerReleaseReason.Compensation)]
        public void ShouldAllowRelease_PreservesVoluntaryAndTerminalVanillaPaths(
            PrivateWarPrisonerReleaseReason reason)
        {
            Assert.True(_policy.ShouldAllowRelease(
                isMainHero: false,
                reason,
                arePrivateEnemies: true));
        }

        [Theory]
        [InlineData(PrivateWarPrisonerReleaseReason.AfterPeace)]
        [InlineData(PrivateWarPrisonerReleaseReason.AfterBattle)]
        public void ShouldAllowRelease_PreservesMainHeroAndOrdinaryCases(
            PrivateWarPrisonerReleaseReason reason)
        {
            Assert.True(_policy.ShouldAllowRelease(
                isMainHero: true,
                reason,
                arePrivateEnemies: true));
            Assert.True(_policy.ShouldAllowRelease(
                isMainHero: false,
                reason,
                arePrivateEnemies: false));
        }
    }
}
