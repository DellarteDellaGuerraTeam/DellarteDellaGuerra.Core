using DellarteDellaGuerra.Domain.PoliticalMap;
using DellarteDellaGuerra.Domain.PoliticalMap.Overlay;

namespace DellarteDellaGuerra.Domain.Tests.PoliticalMap
{
    public class PoliticalMapPaletteTests
    {
        [Fact]
        public void AWashKeepsTheColourAndReplacesItsAlpha()
        {
            Assert.Equal(0x99C02020u, PoliticalMapPalette.Wash(0xFFC02020, 0x99));
            Assert.Equal(((uint)PoliticalMapPalette.WashAlpha << 24) | 0xC02020u, PoliticalMapPalette.Wash(0xFFC02020));
        }

        [Fact]
        public void TheDefaultOpacityIsAboutSixtyPercentStrongEnoughToReadOverSnow()
        {
            Assert.Equal(153, PoliticalMapPalette.AlphaOf(PoliticalMapPalette.DefaultOpacity));
        }

        [Theory]
        [InlineData(0f, 0)]
        [InlineData(1f, 255)]
        [InlineData(0.5f, 128)]
        [InlineData(-2f, 0)]
        [InlineData(7f, 255)]
        [InlineData(float.NaN, 0)]
        public void AnOpacityBecomesAnAlphaByteClampedToTheUnitRange(float opacity, int alpha)
        {
            Assert.Equal(alpha, PoliticalMapPalette.AlphaOf(opacity));
        }

        [Fact]
        public void NoColourWashesToNothing()
        {
            Assert.Equal(0u, PoliticalMapPalette.Wash(null));
        }

        [Fact]
        public void TheFourKingdomsHaveTheirOwnDistinctMapColoursWhateverTheirBanners()
        {
            uint?[] colours = new[] { "sturgia", "vlandia", "khuzait", "battania" }
                .Select(id => PoliticalMapPalette.KingdomColor(id, 0xFFF5F0E0))
                .ToArray();

            Assert.Equal(0xFFD41E1Eu, colours[0]);
            Assert.Equal(0xFF1E64DCu, colours[1]);
            Assert.Equal(0xFF19B333u, colours[2]);
            Assert.Equal(0xFFF20DB9u, colours[3]);
            Assert.DoesNotContain((uint?)0xFFF5F0E0, colours);
            Assert.Equal(4, colours.Distinct().Count());
        }

        [Fact]
        public void TheFourKingdomColoursAreSaturatedAndClearlyApart()
        {
            string[] kingdoms = { "sturgia", "vlandia", "khuzait", "battania" };
            uint[] colours = kingdoms.Select(id => PoliticalMapPalette.KingdomColor(id, null)!.Value).ToArray();
            double[] hues = colours
                .Select(argb =>
                {
                    (double hue, double saturation) = HueAndSaturation(argb);
                    Assert.True(saturation > 0.6, $"{argb:X8} is too grey to read as a wash");
                    return hue;
                })
                .ToArray();

            for (int i = 0; i < hues.Length; i++)
            {
                for (int j = i + 1; j < hues.Length; j++)
                {
                    Assert.True(HueApart(hues[i], hues[j]) >= 40, $"hues {hues[i]:F0} and {hues[j]:F0} are too close");
                    Assert.True(PoliticalMapColouring.Distance(colours[i], colours[j]) >= 70, $"{kingdoms[i]} and {kingdoms[j]} look alike");
                }
            }
        }

        [Theory]
        [InlineData("sturgia", "vlandia")]
        [InlineData("khuzait", "battania")]
        public void RivalKingdomsAreAtLeastAHundredAndTwentyDegreesOfHueAndAHundredDeltaEApart(string kingdom, string rival)
        {
            uint ours = PoliticalMapPalette.KingdomColor(kingdom, null)!.Value;
            uint theirs = PoliticalMapPalette.KingdomColor(rival, null)!.Value;

            Assert.True(HueApart(HueAndSaturation(ours).Hue, HueAndSaturation(theirs).Hue) >= 120);
            Assert.True(PoliticalMapColouring.Distance(ours, theirs) >= 100);
        }

        private static double HueApart(double first, double second)
        {
            double apart = Math.Abs(first - second) % 360;
            return Math.Min(apart, 360 - apart);
        }

        private static (double Hue, double Saturation) HueAndSaturation(uint argb)
        {
            double r = ((argb >> 16) & 0xFF) / 255.0, g = ((argb >> 8) & 0xFF) / 255.0, b = (argb & 0xFF) / 255.0;
            double max = Math.Max(r, Math.Max(g, b)), min = Math.Min(r, Math.Min(g, b)), delta = max - min;
            double hue = delta == 0 ? 0
                : max == r ? 60 * (((g - b) / delta + 6) % 6)
                : max == g ? 60 * ((b - r) / delta + 2)
                : 60 * ((r - g) / delta + 4);
            return (hue, max == 0 ? 0 : delta / max);
        }

        [Fact]
        public void AnyOtherFactionKeepsItsLiveColour()
        {
            Assert.Equal(0xFF123456u, PoliticalMapPalette.KingdomColor("clan_neville", 0xFF123456));
            Assert.Null(PoliticalMapPalette.KingdomColor("clan_neville", null));
        }

        [Fact]
        public void ASignatureListsTheFeudalFillTheHatchThenEachSettlement()
        {
            IReadOnlyList<uint> signature = PoliticalMapPalette.Signature(
                0xFF112233,
                null,
                new (uint?, bool)[] { (0xFF445566, false), (null, true) });

            Assert.Equal(
                new[]
                {
                    PoliticalMapPalette.Wash(0xFF112233), 0u,
                    PoliticalMapPalette.Wash(0xFF445566), 0u,
                    PoliticalMapPalette.Wash(null), 1u
                },
                signature);
        }

        [Fact]
        public void AContestedCountySignsItsOccupantsColour()
        {
            IReadOnlyList<uint> signature = PoliticalMapPalette.Signature(0xFF112233, 0xFF778899, Array.Empty<(uint?, bool)>());

            Assert.Equal(new[] { PoliticalMapPalette.Wash(0xFF112233), PoliticalMapPalette.Wash(0xFF778899) }, signature);
        }

        [Fact]
        public void TheLedgerReportsANewTerritoryAndAChangedOneButNotAnUnchangedOne()
        {
            var ledger = new PoliticalMapPaintLedger();

            Assert.True(ledger.Record("county_kent", new[] { 1u, 0u }));
            Assert.False(ledger.Record("county_kent", new[] { 1u, 0u }));
            Assert.True(ledger.Record("county_kent", new[] { 2u, 0u }));
        }

        [Fact]
        public void AClearedLedgerTreatsEveryTerritoryAsNew()
        {
            var ledger = new PoliticalMapPaintLedger();
            ledger.Record("county_kent", new[] { 1u });

            ledger.Clear();

            Assert.True(ledger.Record("county_kent", new[] { 1u }));
        }
    }
}
