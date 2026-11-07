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
            View == view && Level == level && Mode == mode;

        /// <summary>Derives the selected wash from fresh paints; frontiers and opacity stay unchanged.</summary>
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

        private static uint Darken(uint colour) =>
            (colour & 0xFF000000u) | (((colour >> 16 & 0xFFu) * 3 / 5) << 16)
            | (((colour >> 8 & 0xFFu) * 3 / 5) << 8) | ((colour & 0xFFu) * 3 / 5);
    }
}
