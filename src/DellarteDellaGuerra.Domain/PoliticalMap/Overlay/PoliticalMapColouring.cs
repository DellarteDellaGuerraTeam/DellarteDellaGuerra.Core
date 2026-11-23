using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace DellarteDellaGuerra.Domain.PoliticalMap.Overlay
{
    /// <summary>
    /// The map's own colours and how regions are given them, CK3 style: in the owners view each
    /// region is a lighter or darker shade of its kingdom's colour. Banner colours are never used:
    /// POC gives most English clans white backgrounds and many Scottish ones near-identical reds.
    /// </summary>
    /// <remarks>
    /// Colours are compared by CIE76 distance (Delta E in CIELAB), which follows what the eye sees
    /// far better than RGB distance does: 2 is a just-noticeable difference, 20 is clearly
    /// different. Regions get colours by greedy graph colouring over which regions touch
    /// (<see cref="Assign"/>), so neighbours never look alike.
    /// </remarks>
    public static class PoliticalMapColouring
    {
        /// <summary>
        /// Delta E two touching regions of one kingdom must keep. Shades differ by lightness alone, so
        /// this is lower than between hues: every kingdom's first three shades are 17 or more apart.
        /// </summary>
        public const double ShadeMinDistance = 12;

        /// <summary>Delta E two touching kingdomless clans must keep.</summary>
        public const double DistinctMinDistance = 20;

        /// <summary>How many shades a kingdom has (<see cref="Shades"/>).</summary>
        public const int ShadeCount = 6;

        /// <summary>How far, in HSL lightness, a shade may be from its kingdom's colour.</summary>
        public const double ShadeLightnessBand = 0.2;

        /// <summary>
        /// 26 colours for clans under no kingdom, as opaque ARGB. Picked offline by farthest-point
        /// sampling from HSL hues at least 25 degrees from each of the four kingdom hues, keeping
        /// Delta E 20 or more from every kingdom shade and from each other, so a kingdomless clan's
        /// land never reads as a kingdom's.
        /// </summary>
        public static readonly IReadOnlyList<uint> KingdomlessPalette = new uint[]
        {
            0xFFE8B92C, 0xFF2014FF, 0xFF14D0FF, 0xFF4FFF14, 0xFF681F7A, 0xFF00C27E, 0xFF7A481F, 0xFFD83DFF,
            0xFFEB6A00, 0xFF78AE13, 0xFF1F7A71, 0xFFF3FF14, 0xFF080099, 0xFF14FFF3, 0xFF7A7A1F, 0xFFD89E64,
            0xFFC164D8, 0xFF7751EC, 0xFFB1FF3D, 0xFFD2D864, 0xFF2FBCBC, 0xFF8F13AE, 0xFFAE6113, 0xFF138FAE,
            0xFF14FFA1, 0xFF0F8A59
        };

        // Shades keep their kingdom's HSL hue and saturation and only move lightness, in steps of
        // 0.01 within the band, never darker than 0.22 or paler than 0.72.
        private const double ShadeLightnessStep = 0.01;
        private const double ShadeDarkest = 0.22;
        private const double ShadePalest = 0.72;

        private static readonly ConcurrentDictionary<uint, IReadOnlyList<uint>> ShadeCache = new();

        /// <summary>
        /// <see cref="ShadeCount"/> shades of a colour, as opaque ARGB, the colour itself first. All
        /// keep its HSL hue and saturation and differ by lightness only, within
        /// <see cref="ShadeLightnessBand"/> of its own. The others are picked by farthest-point
        /// sampling, so each new shade is as far as possible (in Delta E) from all those before it;
        /// touching clans of a kingdom therefore take the most different shades first.
        /// </summary>
        public static IReadOnlyList<uint> Shades(uint baseArgb) =>
            ShadeCache.GetOrAdd(baseArgb | 0xFF000000u, BuildShades);

        /// <summary>
        /// Greedy graph colouring. Ids are coloured most weight first, then by id, each taking its
        /// last colour when that still keeps <paramref name="minDistance"/> from every touching id
        /// coloured before it, else its first candidate that does, else the candidate furthest from
        /// them all. The result does not depend on the order ids or pairs are given in.
        /// </summary>
        /// <param name="weights">Every id to colour, with its weight (how many settlements it paints).</param>
        /// <param name="neighbours">Pairs of ids whose regions touch; ids may also be fixed-colour ones.</param>
        /// <param name="candidatesOf">The colours an id may take, in order of preference.</param>
        /// <param name="fixedColours">Ids whose colour is already set (the kingdoms' map colours).</param>
        /// <param name="previous">Last time's colours, kept where they still fit; null for none.</param>
        /// <returns>Each id's colour, plus the fixed colours.</returns>
        public static IReadOnlyDictionary<string, uint> Assign(
            IReadOnlyDictionary<string, int> weights,
            IEnumerable<(string A, string B)> neighbours,
            Func<string, IReadOnlyList<uint>> candidatesOf,
            IReadOnlyDictionary<string, uint> fixedColours,
            IReadOnlyDictionary<string, uint>? previous,
            double minDistance)
        {
            var adjacent = new Dictionary<string, HashSet<string>>();
            foreach ((string a, string b) in neighbours)
            {
                if (a == b) continue;
                Link(a, b);
                Link(b, a);
            }

            Dictionary<string, uint> colours = fixedColours.ToDictionary(pair => pair.Key, pair => pair.Value);
            IEnumerable<string> order = weights.Keys
                .Where(id => !fixedColours.ContainsKey(id))
                .OrderByDescending(id => weights[id])
                .ThenBy(id => id, StringComparer.Ordinal);
            foreach (string id in order)
            {
                List<uint> taken = adjacent.TryGetValue(id, out HashSet<string>? around)
                    ? around.Where(colours.ContainsKey).Select(other => colours[other]).ToList()
                    : new List<uint>();

                IReadOnlyList<uint> palette = candidatesOf(id);
                IEnumerable<uint> candidates = previous is not null && previous.TryGetValue(id, out uint kept) && palette.Contains(kept)
                    ? new[] { kept }.Concat(palette)
                    : palette;
                uint? free = candidates.Cast<uint?>().FirstOrDefault(colour => taken.All(other => Distance(colour!.Value, other) >= minDistance));
                colours[id] = free ?? palette.OrderByDescending(colour => taken.Count == 0 ? 0 : taken.Min(other => Distance(colour, other))).First();
            }

            return colours;

            void Link(string from, string to)
            {
                if (!adjacent.TryGetValue(from, out HashSet<string>? set)) adjacent[from] = set = new HashSet<string>();
                set.Add(to);
            }
        }

        /// <summary>The CIE76 colour difference (Delta E) between two colours' RGB, alpha ignored.</summary>
        public static double Distance(uint first, uint second)
        {
            (double l1, double a1, double b1) = Lab(first);
            (double l2, double a2, double b2) = Lab(second);
            return Math.Sqrt((l1 - l2) * (l1 - l2) + (a1 - a2) * (a1 - a2) + (b1 - b2) * (b1 - b2));
        }

        /// <summary>A colour's CIELAB lightness L*, 0 (black) to 100 (white).</summary>
        public static double Lightness(uint argb) => Lab(argb).L;

        /// <summary>A colour's HSL hue in degrees (0 to 360) and saturation (0 to 1).</summary>
        public static (double Hue, double Saturation) HueAndSaturation(uint argb)
        {
            (double hue, double saturation, double _) = Hsl(argb);
            return (hue, saturation);
        }

        /// <summary>A colour's HSL lightness, 0 (black) to 1 (white).</summary>
        public static double HslLightness(uint argb) => Hsl(argb).Lightness;

        /// <summary>An opaque ARGB colour from HSL: hue in degrees, saturation and lightness 0 to 1.</summary>
        public static uint FromHsl(double hue, double saturation, double lightness)
        {
            hue = ((hue % 360) + 360) % 360 / 360;
            double q = lightness < 0.5 ? lightness * (1 + saturation) : lightness + saturation - lightness * saturation;
            double p = 2 * lightness - q;
            return 0xFF000000u | (Channel(hue + 1.0 / 3) << 16) | (Channel(hue) << 8) | Channel(hue - 1.0 / 3);

            uint Channel(double t)
            {
                if (t < 0) t += 1;
                if (t > 1) t -= 1;
                double value = t < 1.0 / 6 ? p + (q - p) * 6 * t
                    : t < 0.5 ? q
                    : t < 2.0 / 3 ? p + (q - p) * (2.0 / 3 - t) * 6
                    : p;
                return (uint)Math.Round(Math.Max(0, Math.Min(1, value)) * 255);
            }
        }

        private static IReadOnlyList<uint> BuildShades(uint baseArgb)
        {
            (double hue, double saturation, double baseLightness) = Hsl(baseArgb);
            var candidates = new List<uint>();
            int steps = (int)Math.Round(ShadeLightnessBand / ShadeLightnessStep);
            for (int step = -steps; step <= steps; step++)
            {
                double lightness = baseLightness + step * ShadeLightnessStep;
                if (lightness < ShadeDarkest || lightness > ShadePalest) continue;
                candidates.Add(FromHsl(hue, saturation, lightness));
            }

            var shades = new List<uint> { baseArgb };
            while (shades.Count < ShadeCount)
            {
                uint best = candidates.OrderByDescending(candidate => shades.Min(shade => Distance(candidate, shade))).First();
                shades.Add(best);
            }

            return shades;
        }

        private static (double Hue, double Saturation, double Lightness) Hsl(uint argb)
        {
            double r = ((argb >> 16) & 0xFF) / 255.0, g = ((argb >> 8) & 0xFF) / 255.0, b = (argb & 0xFF) / 255.0;
            double max = Math.Max(r, Math.Max(g, b)), min = Math.Min(r, Math.Min(g, b)), delta = max - min;
            double lightness = (max + min) / 2;
            if (delta == 0) return (0, 0, lightness);

            double saturation = delta / (1 - Math.Abs(2 * lightness - 1));
            double hue = max == r ? 60 * (((g - b) / delta + 6) % 6)
                : max == g ? 60 * ((b - r) / delta + 2)
                : 60 * ((r - g) / delta + 4);
            return (hue, saturation, lightness);
        }

        private static (double L, double A, double B) Lab(uint argb)
        {
            double r = Linear((argb >> 16) & 0xFF), g = Linear((argb >> 8) & 0xFF), b = Linear(argb & 0xFF);
            double x = (0.4124 * r + 0.3576 * g + 0.1805 * b) / 0.95047;
            double y = 0.2126 * r + 0.7152 * g + 0.0722 * b;
            double z = (0.0193 * r + 0.1192 * g + 0.9505 * b) / 1.08883;
            double fx = F(x), fy = F(y), fz = F(z);
            return (116 * fy - 16, 500 * (fx - fy), 200 * (fy - fz));

            static double Linear(uint channel)
            {
                double c = channel / 255.0;
                return c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
            }

            static double F(double t) => t > 0.008856 ? Math.Pow(t, 1.0 / 3) : 7.787 * t + 16.0 / 116;
        }
    }
}
