using System;
using System.Collections.Generic;
using System.Linq;

namespace DellarteDellaGuerra.Domain.PoliticalMap.Overlay
{
    /// <summary>One legend swatch: a faction that controls land, in the colour it is painted (or, in the owners view, whose shades its clans are).</summary>
    public sealed record PoliticalMapLegendEntry(string FactionId, string Name, uint Argb, int SettlementCount);

    /// <summary>The kingdom and owners views' legend: every faction controlling at least one settlement on the map.</summary>
    public static class PoliticalMapLegend
    {
        /// <summary>
        /// The factions controlling the given settlements, largest first (ties by name), each in the
        /// swatch colour the view last painted it with (<paramref name="mapColours"/>, from
        /// <see cref="PoliticalMapPainter.MapColours"/>: a kingdom's base colour in the owners view). A faction with no painted colour is not
        /// listed, so the legend is empty until the view has been painted; one with no name is
        /// listed under its id.
        /// </summary>
        public static IReadOnlyList<PoliticalMapLegendEntry> KingdomEntries(
            IEnumerable<string> settlementIds,
            IPoliticalMapWorld world,
            IPoliticalMapNames names,
            IReadOnlyDictionary<string, uint> mapColours)
        {
            var counts = new Dictionary<string, int>();
            foreach (string settlementId in settlementIds)
            {
                if (world.FactionOf(settlementId) is not { } faction) continue;
                counts[faction] = counts.TryGetValue(faction, out int count) ? count + 1 : 1;
            }

            return counts
                .Select(pair => (pair, colour: mapColours.TryGetValue(pair.Key, out uint colour) ? colour : (uint?)null))
                .Where(entry => entry.colour is not null)
                .Select(entry => new PoliticalMapLegendEntry(
                    entry.pair.Key, names.FactionName(entry.pair.Key) ?? entry.pair.Key, entry.colour!.Value, entry.pair.Value))
                .OrderByDescending(entry => entry.SettlementCount)
                .ThenBy(entry => entry.Name, StringComparer.Ordinal)
                .ToList();
        }
    }
}
