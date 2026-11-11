using DellarteDellaGuerra.Domain.PoliticalMap.Overlay;

namespace DellarteDellaGuerra.Domain.Tests.PoliticalMap.Overlay;

public class PoliticalMapLightingTests
{
    [Fact]
    public void DaylightIsUnchangedAndNightIsDimmedWithoutChangingAlphaOrHue()
    {
        Assert.Equal(0xFFFFFFFFu, PoliticalMapLighting.FactorFor(12));
        uint night = PoliticalMapLighting.FactorFor(0);
        Assert.Equal(night, PoliticalMapLighting.FactorFor(24));
        Assert.Equal(night, PoliticalMapLighting.FactorFor(-1));
        Assert.Equal(255u, night >> 24);
        Assert.InRange(night & 255, 160u, 170u);
        Assert.Equal(night & 255, night >> 8 & 255);
        Assert.Equal(night & 255, night >> 16 & 255);
    }

    [Fact]
    public void DawnAndDuskFadeGraduallyAndSymmetrically()
    {
        uint previous = 0;
        for (float hour = 6; hour <= 8; hour += 0.05f)
        {
            uint brightness = PoliticalMapLighting.FactorFor(hour) & 255;
            Assert.True(brightness >= previous);
            Assert.InRange(Math.Abs((int)brightness - (int)(PoliticalMapLighting.FactorFor(26 - hour) & 255)), 0, 1);
            previous = brightness;
        }
        Assert.Equal(0xFFFFFFFFu, PoliticalMapLighting.FactorFor(float.NaN));
    }
}
