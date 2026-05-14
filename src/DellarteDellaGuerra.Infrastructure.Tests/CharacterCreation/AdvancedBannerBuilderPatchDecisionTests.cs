using DellarteDellaGuerra.Infrastructure.CharacterCreation.Patches;

namespace DellarteDellaGuerra.Infrastructure.Tests.CharacterCreation;

public class AdvancedBannerBuilderPatchDecisionTests
{
    [Fact]
    public void ShouldRedirectState_WhenFeatureDisabled_ReturnsFalse()
    {
        var result = RedirectBannerEditorStateToBannerBuilderPatch.ShouldRedirectState(
            featureEnabled: false,
            isBannerEditorState: true,
            hasEndActionCallback: false);

        Assert.False(result);
    }

    [Fact]
    public void ShouldRedirectState_WhenStateIsNotBannerEditor_ReturnsFalse()
    {
        var result = RedirectBannerEditorStateToBannerBuilderPatch.ShouldRedirectState(
            featureEnabled: true,
            isBannerEditorState: false,
            hasEndActionCallback: false);

        Assert.False(result);
    }

    [Fact]
    public void ShouldRedirectState_WhenBannerEditorHasNoCallback_ReturnsTrue()
    {
        var result = RedirectBannerEditorStateToBannerBuilderPatch.ShouldRedirectState(
            featureEnabled: true,
            isBannerEditorState: true,
            hasEndActionCallback: false);

        Assert.True(result);
    }

    [Fact]
    public void ShouldRedirectState_WhenEndCallbackExists_ReturnsFalse()
    {
        var result = RedirectBannerEditorStateToBannerBuilderPatch.ShouldRedirectState(
            featureEnabled: true,
            isBannerEditorState: true,
            hasEndActionCallback: true);

        Assert.False(result);
    }

    [Theory]
    [InlineData(false, false, true, true, false)]
    [InlineData(true, true, true, true, false)]
    [InlineData(true, false, false, true, false)]
    [InlineData(true, false, true, false, false)]
    [InlineData(true, false, true, true, true)]
    public void ShouldPersistResult_RespectsGuards(
        bool featureEnabled,
        bool isCancel,
        bool hasPlayerClan,
        bool hasBanner,
        bool expected)
    {
        var result = PersistBannerBuilderResultPatch.ShouldPersistResult(
            featureEnabled,
            isCancel,
            hasPlayerClan,
            hasBanner);

        Assert.Equal(expected, result);
    }
}
