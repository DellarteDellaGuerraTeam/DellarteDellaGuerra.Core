using System;
using System.Collections.Generic;

namespace DellarteDellaGuerra.Domain.PoliticalMap
{
    /// <summary>
    /// The political map's colours, as the ARGB vertex colours its <c>vertex_color_mat</c> copy draws.
    /// The material ignores entity and mesh alpha, so the wash's translucency lives in the alpha
    /// byte, alpha-blended over the terrain (the renderer sets the copy to <c>Modulate</c>).
    /// </summary>
    public static class PoliticalMapPalette
    {
        /// <summary>
        /// 60%: strong enough to read over snow and bright terrain, while rivers and relief still
        /// show through. 1 hides the terrain under painted land, a flat paper map.
        /// </summary>
        public const float DefaultOpacity = 0.6f;

        private static byte _washAlpha = AlphaOf(DefaultOpacity);

        /// <summary>
        /// The fill opacity every wash is drawn at, 0 to 1. One setting for the whole map
        /// (<c>polmap.opacity</c>): changing it changes every paint, so the next repaint redraws all.
        /// </summary>
        public static float Opacity
        {
            get => _washAlpha / 255f;
            set => _washAlpha = AlphaOf(value);
        }

        /// <summary>The fill's alpha byte at the current <see cref="Opacity"/>.</summary>
        public static byte WashAlpha => _washAlpha;

        /// <summary>An opacity as an alpha byte, clamped to 0..1 (NaN counts as 0).</summary>
        public static byte AlphaOf(float opacity) =>
            float.IsNaN(opacity) ? (byte)0 : (byte)Math.Round(Math.Max(0f, Math.Min(1f, opacity)) * 255f);

        /// <summary>The colour at wash alpha, or 0 (nothing drawn) when there is none.</summary>
        public static uint Wash(uint? argb) => Wash(argb, _washAlpha);

        /// <summary>The colour at the given alpha, or 0 (nothing drawn) when there is none.</summary>
        public static uint Wash(uint? argb, byte alpha) =>
            argb is { } colour ? ((uint)alpha << 24) | (colour & 0x00FFFFFFu) : 0u;

        /// <summary>
        /// The map's own kingdom colours. A kingdom's live colour follows its POC banner, which gives
        /// York and Lancaster near-white backgrounds, so the map keeps four hues of its own. The 1471
        /// scenario is two civil wars inside one war, so each rival pair is set on opposite sides of
        /// the colour wheel: Lancaster red (hue 0) against York blue (218), 142 degrees and Delta E
        /// 123 apart; the Crown of Scotland green (130) against the Lordship of the Isles magenta
        /// (315), 175 degrees and Delta E 170 apart. Scotland, which borders England, is Delta E 130
        /// from Lancaster and 148 from York. The Isles touch only Scotland; they are still Delta E 81
        /// from Lancaster and 73 from York.
        /// </summary>
        private static readonly IReadOnlyDictionary<string, uint> KingdomColors = new Dictionary<string, uint>
        {
            ["sturgia"] = 0xFFD41E1E, // House of Lancaster: red
            ["vlandia"] = 0xFF1E64DC, // House of York: blue
            ["khuzait"] = 0xFF19B333, // Crown of Scotland: green
            ["battania"] = 0xFFF20DB9 // Lordship of the Isles: magenta
        };

        /// <summary>A kingdom's map colour: its fixed one when it has one, else its live colour.</summary>
        public static uint? KingdomColor(string kingdomId, uint? liveArgb) =>
            KingdomColors.TryGetValue(kingdomId, out uint colour) ? colour : liveArgb;

        /// <summary>
        /// What a territory's meshes are painted from, flattened to compare one day's state with the
        /// last: the feudal fill (the county holder's clan colour), the occupant's hatch colour, or
        /// zero when uncontested, then, for each of its settlements in a stable order, the kingdom
        /// fill (the controlling faction's colour) and whether that faction is at war with the
        /// player's. Both views are covered, so switching views never needs a signature of its own.
        /// </summary>
        public static IReadOnlyList<uint> Signature(
            uint? holderClanArgb,
            uint? occupantClanArgb,
            IEnumerable<(uint? FactionArgb, bool AtWarWithPlayer)> settlements)
        {
            var signature = new List<uint>
            {
                Wash(holderClanArgb),
                occupantClanArgb is null ? 0u : Wash(occupantClanArgb)
            };
            foreach ((uint? factionArgb, bool atWarWithPlayer) in settlements)
            {
                signature.Add(Wash(factionArgb));
                signature.Add(atWarWithPlayer ? 1u : 0u);
            }

            return signature;
        }
    }
}
