using System;
using System.Collections.Generic;
using DellarteDellaGuerra.Domain.PoliticalMap.Overlay;

namespace DellarteDellaGuerra.Domain.PoliticalMap.Labels
{
    /// <summary>What a set of region labels groups land by, from the most local to the widest.</summary>
    public enum PoliticalMapLabelTier
    {
        /// <summary>
        /// The vassal-hierarchy region each settlement is painted in (the owners view), at the
        /// owners view's current level: the clan the region belongs to.
        /// </summary>
        Owner,

        /// <summary>The kingdom or kingdomless clan controlling each settlement (the kingdom view).</summary>
        Faction,

        /// <summary>The configured geographic title region, independent of clan ownership.</summary>
        Territory
    }

    public static class PoliticalMapLabelTiers
    {
        private static readonly PoliticalMapLabelTier[] None = Array.Empty<PoliticalMapLabelTier>();
        private static readonly PoliticalMapLabelTier[] Kingdom = { PoliticalMapLabelTier.Faction };
        private static readonly PoliticalMapLabelTier[] Owners = { PoliticalMapLabelTier.Owner };
        private static readonly PoliticalMapLabelTier[] Territories = { PoliticalMapLabelTier.Territory };

        /// <summary>The tiers a view labels; each at every zoom.</summary>
        public static IReadOnlyList<PoliticalMapLabelTier> Of(PoliticalMapView view, PoliticalMapLabelMode mode = PoliticalMapLabelMode.Clans) => view switch
        {
            PoliticalMapView.Kingdom => Kingdom,
            PoliticalMapView.Owners => mode == PoliticalMapLabelMode.Titles ? Territories : Owners,
            PoliticalMapView.DeJure => Territories,
            _ => None
        };

        /// <summary>Regions smaller than this many cells are fragments and get no label.</summary>
        public static int MinCells(PoliticalMapLabelTier tier) => tier switch
        {
            PoliticalMapLabelTier.Owner or PoliticalMapLabelTier.Territory => 150,
            _ => 400
        };

        /// <summary>
        /// Each settlement's key for a tier, by its index in <paramref name="settlementIds"/>. The
        /// owner tier reads the owners view's region keys at its current level
        /// (<paramref name="ownerRegionKeys"/>, by the same index), so the labels group land exactly
        /// as the colours do.
        /// </summary>
        public static string?[] KeysOf(
            PoliticalMapLabelTier tier,
            IReadOnlyList<string> settlementIds,
            IReadOnlyList<string?>? ownerRegionKeys,
            IPoliticalMapWorld world)
        {
            var keys = new string?[settlementIds.Count];
            for (int index = 0; index < keys.Length; index++)
            {
                keys[index] = tier switch
                {
                    PoliticalMapLabelTier.Faction => world.FactionOf(settlementIds[index]),
                    PoliticalMapLabelTier.Owner or PoliticalMapLabelTier.Territory => ownerRegionKeys is not null && index < ownerRegionKeys.Count ? ownerRegionKeys[index] : null,
                    _ => null
                };
            }

            return keys;
        }
    }
}
