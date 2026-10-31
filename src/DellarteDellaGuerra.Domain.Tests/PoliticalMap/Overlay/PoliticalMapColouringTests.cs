using DellarteDellaGuerra.Domain.PoliticalMap;
using DellarteDellaGuerra.Domain.PoliticalMap.Grid;
using DellarteDellaGuerra.Domain.PoliticalMap.Overlay;

namespace DellarteDellaGuerra.Domain.Tests.PoliticalMap.Overlay
{
    public class PoliticalMapColouringTests
    {
        private static readonly Dictionary<string, uint> NoFixed = new();

        private static readonly string[] Kingdoms = { "sturgia", "vlandia", "khuzait", "battania" };

        // A ring of six regions, each touching the next, plus a hub touching them all.
        private static readonly Dictionary<string, int> Counts = new()
        {
            ["hub"] = 6, ["a"] = 3, ["b"] = 3, ["c"] = 2, ["d"] = 2, ["e"] = 1, ["f"] = 1
        };

        private static readonly (string A, string B)[] Touching =
        {
            ("a", "b"), ("b", "c"), ("c", "d"), ("d", "e"), ("e", "f"), ("f", "a"),
            ("hub", "a"), ("hub", "b"), ("hub", "c"), ("hub", "d"), ("hub", "e"), ("hub", "f")
        };

        private static uint KingdomColour(string id) => PoliticalMapPalette.KingdomColor(id, null)!.Value;

        private static double HueApart(double first, double second)
        {
            double apart = Math.Abs(first - second) % 360;
            return Math.Min(apart, 360 - apart);
        }

        private static IReadOnlyDictionary<string, uint> Assign(
            Func<string, IReadOnlyList<uint>> candidatesOf, double minDistance, IReadOnlyDictionary<string, uint>? previous = null) =>
            PoliticalMapColouring.Assign(Counts, Touching, candidatesOf, NoFixed, previous, minDistance);

        [Theory]
        [InlineData("sturgia")]
        [InlineData("vlandia")]
        [InlineData("khuzait")]
        [InlineData("battania")]
        public void EachKingdomsShadesKeepItsHueAndSaturationAndDifferOnlyByLightness(string kingdom)
        {
            uint colour = KingdomColour(kingdom);
            IReadOnlyList<uint> shades = PoliticalMapColouring.Shades(colour);

            Assert.Equal(PoliticalMapColouring.ShadeCount, shades.Count);
            Assert.Equal(colour, shades[0]);
            (double hue, double saturation) = PoliticalMapColouring.HueAndSaturation(colour);
            double lightness = PoliticalMapColouring.HslLightness(colour);
            Assert.All(shades, shade =>
            {
                Assert.Equal(0xFFu, shade >> 24);
                (double shadeHue, double shadeSaturation) = PoliticalMapColouring.HueAndSaturation(shade);
                // Only 8-bit rounding may move the hue or saturation.
                Assert.True(HueApart(hue, shadeHue) <= 3, $"{shade:X8} drifts off the kingdom's hue");
                Assert.True(Math.Abs(saturation - shadeSaturation) <= 0.05, $"{shade:X8} changes the kingdom's saturation");
                Assert.True(Math.Abs(lightness - PoliticalMapColouring.HslLightness(shade)) <= PoliticalMapColouring.ShadeLightnessBand + 0.01,
                    $"{shade:X8} leaves the lightness band");
            });
            Assert.Equal(shades.Count, shades.Distinct().Count());
            // The first three, which touching clans take first, are clearly apart.
            for (int first = 0; first < 3; first++)
            {
                for (int second = first + 1; second < 3; second++)
                {
                    Assert.True(PoliticalMapColouring.Distance(shades[first], shades[second]) >= PoliticalMapColouring.ShadeMinDistance,
                        $"{shades[first]:X8} and {shades[second]:X8} look alike");
                }
            }
        }

        [Theory]
        [InlineData("sturgia", "vlandia")]
        [InlineData("khuzait", "battania")]
        public void NoShadeOfARivalKingdomIsAnywhereNearTheOthers(string kingdom, string rival)
        {
            IReadOnlyList<uint> ours = PoliticalMapColouring.Shades(KingdomColour(kingdom));
            IReadOnlyList<uint> theirs = PoliticalMapColouring.Shades(KingdomColour(rival));

            Assert.All(ours, shade => Assert.All(theirs, other =>
                Assert.True(PoliticalMapColouring.Distance(shade, other) >= 60, $"{shade:X8} and {other:X8} are too close for enemies")));
        }

        [Fact]
        public void NoShadeOfOneKingdomIsAShadeOfAnother()
        {
            IReadOnlyList<uint>[] families = Kingdoms.Select(id => PoliticalMapColouring.Shades(KingdomColour(id))).ToArray();

            for (int first = 0; first < families.Length; first++)
            {
                for (int second = first + 1; second < families.Length; second++)
                {
                    Assert.Empty(families[first].Intersect(families[second]));
                }
            }
        }

        [Fact]
        public void TouchingClansOfOneKingdomGetClearlyDifferentShades()
        {
            IReadOnlyList<uint> shades = PoliticalMapColouring.Shades(KingdomColour("vlandia"));

            IReadOnlyDictionary<string, uint> colours = Assign(_ => shades, PoliticalMapColouring.ShadeMinDistance);

            Assert.Equal(KingdomColour("vlandia"), colours["hub"]);
            Assert.All(colours.Values, colour => Assert.Contains(colour, shades));
            Assert.All(Touching, pair =>
                Assert.True(PoliticalMapColouring.Distance(colours[pair.A], colours[pair.B]) >= PoliticalMapColouring.ShadeMinDistance));
        }

        [Fact]
        public void TheKingdomlessPaletteHasTwentyFourOrMoreDistinctColoursAwayFromEveryKingdom()
        {
            IReadOnlyList<uint> palette = PoliticalMapColouring.KingdomlessPalette;
            uint[] kingdomShades = Kingdoms.SelectMany(id => PoliticalMapColouring.Shades(KingdomColour(id))).ToArray();
            double[] kingdomHues = Kingdoms.Select(id => PoliticalMapColouring.HueAndSaturation(KingdomColour(id)).Hue).ToArray();

            Assert.True(palette.Count >= 24);
            Assert.Equal(palette.Count, palette.Distinct().Count());
            Assert.All(palette, colour =>
            {
                double hue = PoliticalMapColouring.HueAndSaturation(colour).Hue;
                Assert.All(kingdomHues, kingdomHue => Assert.True(HueApart(hue, kingdomHue) >= 20, $"{colour:X8} is on a kingdom's hue"));
                Assert.All(kingdomShades, shade =>
                    Assert.True(PoliticalMapColouring.Distance(colour, shade) >= PoliticalMapColouring.DistinctMinDistance,
                        $"{colour:X8} looks like the kingdom shade {shade:X8}"));
            });
            for (int first = 0; first < palette.Count; first++)
            {
                for (int second = first + 1; second < palette.Count; second++)
                {
                    Assert.True(PoliticalMapColouring.Distance(palette[first], palette[second]) >= PoliticalMapColouring.DistinctMinDistance);
                }
            }
        }

        [Fact]
        public void TheSameMapGetsTheSameColoursWhateverOrderItIsGivenIn()
        {
            Func<string, IReadOnlyList<uint>> palette = _ => PoliticalMapColouring.KingdomlessPalette;
            IReadOnlyDictionary<string, uint> once = Assign(palette, PoliticalMapColouring.DistinctMinDistance);
            var shuffledCounts = Counts.Reverse().ToDictionary(pair => pair.Key, pair => pair.Value);
            var shuffledTouching = Touching.Reverse().Select(pair => (pair.B, pair.A));

            IReadOnlyDictionary<string, uint> again = PoliticalMapColouring.Assign(
                shuffledCounts, shuffledTouching, palette, NoFixed, null, PoliticalMapColouring.DistinctMinDistance);

            Assert.All(Counts.Keys, id => Assert.Equal(once[id], again[id]));
        }

        [Fact]
        public void AnIdKeepsItsLastColourWhileItStillFits()
        {
            uint last = PoliticalMapColouring.KingdomlessPalette[20];
            var previous = new Dictionary<string, uint> { ["e"] = last };

            IReadOnlyDictionary<string, uint> colours = Assign(_ => PoliticalMapColouring.KingdomlessPalette, PoliticalMapColouring.DistinctMinDistance, previous);

            Assert.Equal(last, colours["e"]);
        }

        [Fact]
        public void AnIdGivesUpItsLastColourWhenABiggerNeighbourNowHasIt()
        {
            uint first = PoliticalMapColouring.KingdomlessPalette[0];
            var previous = new Dictionary<string, uint> { ["a"] = first };

            IReadOnlyDictionary<string, uint> colours = Assign(_ => PoliticalMapColouring.KingdomlessPalette, PoliticalMapColouring.DistinctMinDistance, previous);

            Assert.Equal(first, colours["hub"]);
            Assert.NotEqual(first, colours["a"]);
        }

        [Fact]
        public void AFixedColourIsKeptAndItsNeighboursKeptOffIt()
        {
            uint lancaster = KingdomColour("sturgia");
            var candidates = new uint[] { lancaster, 0xFF1A8FD0 };

            IReadOnlyDictionary<string, uint> colours = PoliticalMapColouring.Assign(
                new Dictionary<string, int> { ["sturgia"] = 9, ["clan_rebel"] = 1 },
                new[] { ("clan_rebel", "sturgia") }, _ => candidates, new Dictionary<string, uint> { ["sturgia"] = lancaster }, null,
                PoliticalMapColouring.DistinctMinDistance);

            Assert.Equal(lancaster, colours["sturgia"]);
            Assert.Equal(0xFF1A8FD0u, colours["clan_rebel"]);
        }

        [Fact]
        public void WithEveryColourTakenTheFurthestOneWins()
        {
            var fixedColours = new Dictionary<string, uint> { ["near"] = 0xFF101010 };
            var candidates = new uint[] { 0xFF121212, 0xFFF0F0F0 };

            IReadOnlyDictionary<string, uint> colours = PoliticalMapColouring.Assign(
                new Dictionary<string, int> { ["last"] = 0 }, new[] { ("last", "near") }, _ => candidates, fixedColours, null, 1000);

            Assert.Equal(0xFFF0F0F0u, colours["last"]);
        }

        [Fact]
        public void DeltaEIsZeroForTheSameColourAndAboutAHundredFromBlackToWhite()
        {
            Assert.Equal(0, PoliticalMapColouring.Distance(0xFF336699, 0x00336699), 6);
            Assert.InRange(PoliticalMapColouring.Distance(0xFF000000, 0xFFFFFFFF), 99, 101);
        }

        [Fact]
        public void SettlementsTouchWhenTheirCellsShareASide()
        {
            // 1 1 2
            // 3 . 2
            // 3 3 4   (1 and 4 only meet at a corner, 3 and 4 share a side)
            ushort[] cells = { 1, 1, 2, 3, PoliticalMapGrid.None, 2, 3, 3, 4 };
            var grid = new PoliticalMapGrid(3, 3, 1f, new[] { "one", "two", "three", "four" }, cells);

            Assert.Equal(new[] { (0, 1), (0, 2), (1, 3), (2, 3) }, PoliticalMapSettlementAdjacency.Of(grid));
        }
    }
}
