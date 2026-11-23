using System;
using System.Collections.Generic;
using DellarteDellaGuerra.Domain.PoliticalMap.Labels;

namespace DellarteDellaGuerra.Domain.PoliticalMap.Overlay
{
    /// <summary>One displayed title, clan or kingdom, including all of its disconnected pieces.</summary>
    public sealed record PoliticalMapSelection(
        PoliticalMapView View, PoliticalMapOwnerLevel Level, PoliticalMapLabelMode Mode, string Key)
    {
        public bool Matches(PoliticalMapView view, PoliticalMapOwnerLevel level, PoliticalMapLabelMode mode) =>
            View == view && Mode == mode;

        /// <summary>Derives a darker, more opaque selected wash from fresh paints; frontiers stay unchanged.</summary>
        public PoliticalMapSettlementPaint[] Apply(IReadOnlyList<PoliticalMapSettlementPaint> paints, IReadOnlyList<string?> keys)
        {
            if (paints.Count != keys.Count) throw new ArgumentException("One selection key per settlement is required.", nameof(keys));
            var result = new PoliticalMapSettlementPaint[paints.Count];
            for (int i = 0; i < result.Length; i++)
                result[i] = keys[i] == Key
                    ? paints[i] with { Fill = Darken(paints[i].Fill), Hatch = Darken(paints[i].Hatch) }
                    : paints[i];
            return result;
        }

        private static uint Darken(uint colour) => colour == 0 ? 0 :
            (Math.Max(colour >> 24, 0xD9u) << 24) | (((colour >> 16 & 0xFFu) / 4) << 16)
            | (((colour >> 8 & 0xFFu) / 4) << 8) | ((colour & 0xFFu) / 4);
    }
}
