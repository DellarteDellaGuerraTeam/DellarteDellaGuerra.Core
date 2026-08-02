using Bannerlord.PrivateWars.Domain.Captivity;

namespace DellarteDellaGuerra.Domain.Tests.PrivateWars;

public class CaptivityPolicyTests
{
    private readonly CaptivityPolicy _policy = new();

    [Theory]
    [InlineData(true, false, true, false, false, false)]
    [InlineData(false, true, true, false, false, false)]
    [InlineData(false, false, true, false, false, true)]
    [InlineData(false, false, false, false, false, true)]
    [InlineData(false, false, false, true, false, false)]
    [InlineData(false, false, false, false, true, false)]
    public void ShouldReleaseForNoMoreEnemies_MatchesVanillaExceptForActivePrivateHostility(
        bool vanillaAtWar,
        bool privateEnemies,
        bool sameFaction,
        bool crimeModerate,
        bool crimeSevere,
        bool expected)
    {
        var result = _policy.ShouldReleaseForNoMoreEnemies(
            vanillaAtWar,
            privateEnemies,
            sameFaction,
            crimeModerate,
            crimeSevere);

        Assert.Equal(expected, result);
    }
}
