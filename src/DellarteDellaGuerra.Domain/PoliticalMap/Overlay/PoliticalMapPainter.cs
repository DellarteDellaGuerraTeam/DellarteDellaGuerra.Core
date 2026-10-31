using System;
using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.Titles.Port;

namespace DellarteDellaGuerra.Domain.PoliticalMap.Overlay
{
    /// <summary>
    /// Decides how every settlement's cells are painted in a view. The kingdom view paints each
    /// settlement for its faction; the owners view paints the vassal hierarchy
    /// (<see cref="PoliticalMapVassalHierarchy"/>): each settlement's land goes to its owner clan
    /// and is painted in the region of the clan it answers to at the level the camera is at.
    /// </summary>
    /// <remarks>
    /// Colours, CK3 style (<see cref="PoliticalMapColouring"/>), never a banner colour:
    /// <list type="bullet">
    /// <item>Kingdom: the four kingdoms in their fixed map colours
    /// (<see cref="PoliticalMapPalette.KingdomColor"/>), any other faction a colour from the
    /// kingdomless palette.</item>
    /// <item>Owners: each region a lighter or darker shade of its holder's kingdom's map colour,
    /// touching regions different shades; a kingdomless clan's region a colour from the
    /// kingdomless palette. Each level remembers its own colours.</item>
    /// </list>
    /// A settlement with no owner and land under no faction are left unfilled. The keys are not
    /// drawn; the region labels group by them. In the owners view the region key is the clan
    /// whose region the land is painted in.
    /// </remarks>
    public class PoliticalMapPainter
    {
        private const string SettlementTerritoryPrefix = "settlement:";

        private static readonly IReadOnlyDictionary<string, uint> NoColours = new Dictionary<string, uint>();

        private readonly IFeudalStructure _feudalStructure;
        private readonly ITitleRepository _titleRepository;
        private readonly IGenealogy _genealogy;
        private readonly PoliticalMapTerritories _territories;
        private readonly Dictionary<PoliticalMapView, IReadOnlyDictionary<string, uint>> _legendColours = new();
        private readonly Dictionary<string, IReadOnlyDictionary<string, uint>> _previous = new();

        public PoliticalMapPainter(
            IFeudalStructure feudalStructure,
            ITitleRepository titleRepository,
            IGenealogy genealogy,
            PoliticalMapTerritories territories)
        {
            _feudalStructure = feudalStructure;
            _titleRepository = titleRepository;
            _genealogy = genealogy;
            _territories = territories;
        }

        /// <summary>
        /// The territory a settlement's cells are meshed with, before any demesne fold: its seat's
        /// territory, or the seat itself when it seats no title.
        /// </summary>
        public string TerritoryOf(string settlementId, IPoliticalMapWorld world)
        {
            string seat = world.SeatOf(settlementId) ?? settlementId;
            return _territories.GetTerritoryOfSeat(seat) ?? SettlementTerritoryPrefix + seat;
        }

        public IReadOnlyDictionary<string, string> Fold(Grid.PoliticalMapGrid grid, IReadOnlyList<string?> territoryOfSettlement) =>
            PoliticalMapDemesneFold.Fold(grid, territoryOfSettlement, territory => _feudalStructure.GetRank(territory));

        /// <summary>
        /// The opaque swatch colour of every faction the legend lists for a view, by faction id, as
        /// last painted with its neighbours known (the overlay's paint). Kingdom view: each faction's
        /// map colour. Owners view: each kingdom's base colour (its clans are shades of it) and each
        /// kingdomless clan's own colour.
        /// </summary>
        public IReadOnlyDictionary<string, uint> MapColours(PoliticalMapView view) =>
            _legendColours.TryGetValue(view, out IReadOnlyDictionary<string, uint>? colours) ? colours : NoColours;

        /// <param name="neighbours">
        /// The grid's touching settlement pairs (<see cref="PoliticalMapSettlementAdjacency"/>), so
        /// touching regions get different colours; null when only the keys matter.
        /// </param>
        /// <param name="level">How deep the owners view splits each kingdom; the kingdom view ignores it.</param>
        public PoliticalMapSettlementPaint[] Paint(
            PoliticalMapView view,
            IReadOnlyList<string> settlementIds,
            IReadOnlyList<string> territoryOfSettlement,
            IReadOnlyDictionary<string, string> folds,
            IPoliticalMapWorld world,
            IReadOnlyList<(int A, int B)>? neighbours = null,
            PoliticalMapOwnerLevel level = PoliticalMapOwnerLevel.SubVassals)
        {
            int count = settlementIds.Count;
            var paints = new PoliticalMapSettlementPaint[count];
            if (view == PoliticalMapView.Off)
            {
                for (int index = 0; index < count; index++) paints[index] = PoliticalMapSettlementPaint.Blank;
                return paints;
            }

            neighbours ??= Array.Empty<(int A, int B)>();
            bool remember = neighbours.Count > 0;
            var fills = new uint?[count];
            var keys = new (string County, string? Duchy, string? Realm)[count];
            var regionKeys = new string?[count];
            for (int index = 0; index < count; index++)
            {
                string territory = territoryOfSettlement[index];
                keys[index] = (folds.TryGetValue(territory, out string? folded) ? folded : territory, null, null);
            }

            if (view == PoliticalMapView.Kingdom) PaintKingdoms(settlementIds, world, neighbours, remember, fills, keys);
            else PaintOwners(settlementIds, world, neighbours, remember, level, fills, regionKeys);

            for (int index = 0; index < count; index++)
            {
                paints[index] = new PoliticalMapSettlementPaint(
                    PoliticalMapPalette.Wash(fills[index]), 0,
                    keys[index].County, keys[index].Duchy, keys[index].Realm, regionKeys[index]);
            }

            return paints;
        }

        // Kingdoms in their fixed colours, every other faction from the kingdomless palette.
        private void PaintKingdoms(
            IReadOnlyList<string> settlementIds, IPoliticalMapWorld world, IReadOnlyList<(int A, int B)> neighbours, bool remember,
            uint?[] fills, (string County, string? Duchy, string? Realm)[] keys)
        {
            var factions = new string?[settlementIds.Count];
            var fixedColours = new Dictionary<string, uint>();
            for (int index = 0; index < factions.Length; index++)
            {
                string? faction = factions[index] = world.FactionOf(settlementIds[index]);
                keys[index] = (keys[index].County, null, faction);
                if (faction is not null && PoliticalMapPalette.KingdomColor(faction, null) is { } kingdom) fixedColours[faction] = kingdom;
            }

            IReadOnlyDictionary<string, uint> colours = PoliticalMapColouring.Assign(
                Weights(factions), Touching(factions, neighbours), _ => PoliticalMapColouring.KingdomlessPalette, fixedColours,
                Previous("Kingdom", remember), PoliticalMapColouring.DistinctMinDistance);
            Fill(factions, colours, fills);
            if (!remember) return;

            _previous["Kingdom"] = colours;
            _legendColours[PoliticalMapView.Kingdom] = colours;
        }

        /// <summary>The vassal hierarchy as it stands now, which the owners view paints and names.</summary>
        public PoliticalMapVassalHierarchy HierarchyOf(IPoliticalMapWorld world) =>
            new(_feudalStructure, _titleRepository, _genealogy, world);

        // Each region a shade of its holder's kingdom's colour; kingdomless clans from the kingdomless palette.
        private void PaintOwners(
            IReadOnlyList<string> settlementIds, IPoliticalMapWorld world, IReadOnlyList<(int A, int B)> neighbours, bool remember,
            PoliticalMapOwnerLevel level, uint?[] fills, string?[] regionKeys)
        {
            PoliticalMapVassalHierarchy hierarchy = HierarchyOf(world);
            var holders = new Dictionary<string, string>(StringComparer.Ordinal);
            for (int index = 0; index < regionKeys.Length; index++)
            {
                if (world.OwnerClanOf(settlementIds[index]) is not { } owner) continue;
                if (!holders.TryGetValue(owner, out string? holder)) holders[owner] = holder = hierarchy.RegionHolderOf(owner, level);
                regionKeys[index] = holder;
            }

            var kingdomBase = new Dictionary<string, uint>();
            var kingdomOfHolder = new Dictionary<string, string>();
            foreach (string holder in holders.Values.Distinct())
            {
                if (world.KingdomOfClan(holder) is not { } kingdom) continue;
                if (!kingdomBase.ContainsKey(kingdom))
                {
                    if (PoliticalMapPalette.KingdomColor(kingdom, world.FactionColor(kingdom)) is not { } colour) continue;
                    kingdomBase[kingdom] = colour | 0xFF000000u;
                }

                kingdomOfHolder[holder] = kingdom;
            }

            string tier = "Owners:" + level;
            IReadOnlyDictionary<string, uint> colours = PoliticalMapColouring.Assign(
                Weights(regionKeys), Touching(regionKeys, neighbours),
                holder => kingdomOfHolder.TryGetValue(holder, out string? kingdom)
                    ? PoliticalMapColouring.Shades(kingdomBase[kingdom])
                    : PoliticalMapColouring.KingdomlessPalette,
                NoColours, Previous(tier, remember), PoliticalMapColouring.ShadeMinDistance);
            Fill(regionKeys, colours, fills);
            if (!remember) return;

            _previous[tier] = colours;
            var legend = new Dictionary<string, uint>(kingdomBase);
            foreach (KeyValuePair<string, uint> pair in colours)
            {
                if (!kingdomOfHolder.ContainsKey(pair.Key)) legend[pair.Key] = pair.Value;
            }

            _legendColours[PoliticalMapView.Owners] = legend;
        }

        private IReadOnlyDictionary<string, uint>? Previous(string tier, bool remember) =>
            remember && _previous.TryGetValue(tier, out IReadOnlyDictionary<string, uint>? colours) ? colours : null;

        private static Dictionary<string, int> Weights(string?[] ids)
        {
            var weights = new Dictionary<string, int>();
            foreach (string? id in ids)
            {
                if (id is not null) weights[id] = weights.TryGetValue(id, out int seen) ? seen + 1 : 1;
            }

            return weights;
        }

        private static IEnumerable<(string A, string B)> Touching(string?[] ids, IReadOnlyList<(int A, int B)> neighbours)
        {
            foreach ((int a, int b) in neighbours)
            {
                if (ids[a] is { } first && ids[b] is { } second && first != second) yield return (first, second);
            }
        }

        private static void Fill(string?[] ids, IReadOnlyDictionary<string, uint> colours, uint?[] fills)
        {
            for (int index = 0; index < ids.Length; index++)
            {
                if (ids[index] is { } id && colours.TryGetValue(id, out uint colour)) fills[index] = colour;
            }
        }
    }
}
