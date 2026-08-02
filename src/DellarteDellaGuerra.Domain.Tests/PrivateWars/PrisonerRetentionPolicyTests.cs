using Bannerlord.PrivateWars.Domain.Captivity;

namespace DellarteDellaGuerra.Domain.Tests.PrivateWars
{
    public class PrisonerRetentionPolicyTests
    {
        private readonly PrisonerRetentionPolicy _policy = new();

        [Theory]
        [InlineData(PrisonerReleaseReason.AfterPeace)]
        [InlineData(PrisonerReleaseReason.AfterBattle)]
        public void ShouldAllowRelease_BlocksInvoluntaryReleaseForPrivateEnemies(
            PrisonerReleaseReason reason)
        {
            Assert.False(_policy.ShouldAllowRelease(
                isMainHero: false,
                reason,
                arePrivateEnemies: true));
        }

        [Theory]
        [InlineData(PrisonerReleaseReason.Ransom)]
        [InlineData(PrisonerReleaseReason.Escape)]
        [InlineData(PrisonerReleaseReason.DeliberateRelease)]
        [InlineData(PrisonerReleaseReason.Death)]
        [InlineData(PrisonerReleaseReason.Compensation)]
        public void ShouldAllowRelease_PreservesVoluntaryAndTerminalVanillaPaths(
            PrisonerReleaseReason reason)
        {
            Assert.True(_policy.ShouldAllowRelease(
                isMainHero: false,
                reason,
                arePrivateEnemies: true));
        }

        [Theory]
        [InlineData(PrisonerReleaseReason.AfterPeace)]
        [InlineData(PrisonerReleaseReason.AfterBattle)]
        public void ShouldAllowRelease_PreservesMainHeroAndOrdinaryCases(
            PrisonerReleaseReason reason)
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
