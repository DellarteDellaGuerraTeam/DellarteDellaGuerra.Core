using System;
using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.PoliticalMap.Overlay;

namespace DellarteDellaGuerra.Domain.PoliticalMap.Labels
{
    /// <summary>One region layout per view and zoom level, for a single grid.</summary>
    public sealed class PoliticalMapLabelRegionCache
    {
        private readonly Entry?[] _entries = new Entry?[5];

        public IReadOnlyList<PoliticalMapRegion>[]? Get(
            PoliticalMapView view, PoliticalMapOwnerLevel level, string?[][] keys)
        {
            int slot = Slot(view, level);
            Entry? entry = slot < 0 ? null : _entries[slot];
            return entry is not null && SameKeys(entry.Keys, keys) ? entry.Regions : null;
        }

        public void Store(
            PoliticalMapView view, PoliticalMapOwnerLevel level, string?[][] keys,
            IReadOnlyList<PoliticalMapRegion>[] regions)
        {
            int slot = Slot(view, level);
            if (slot >= 0) _entries[slot] = new Entry(keys.Select(tier => tier.ToArray()).ToArray(), regions);
        }

        public void Clear() => Array.Clear(_entries, 0, _entries.Length);

        public static bool SameKeys(string?[][] a, string?[][] b)
        {
            if (a.Length != b.Length) return false;
            for (int index = 0; index < a.Length; index++)
            {
                if (!a[index].SequenceEqual(b[index], StringComparer.Ordinal)) return false;
            }

            return true;
        }

        private static int Slot(PoliticalMapView view, PoliticalMapOwnerLevel level) => view switch
        {
            PoliticalMapView.Kingdom => 0,
            PoliticalMapView.Owners => level == PoliticalMapOwnerLevel.Vassals ? 1 : 2,
            PoliticalMapView.DeJure => level == PoliticalMapOwnerLevel.Vassals ? 3 : 4,
            _ => -1
        };

        private sealed class Entry
        {
            public Entry(string?[][] keys, IReadOnlyList<PoliticalMapRegion>[] regions)
            {
                Keys = keys;
                Regions = regions;
            }

            public string?[][] Keys { get; }
            public IReadOnlyList<PoliticalMapRegion>[] Regions { get; }
        }
    }
}
