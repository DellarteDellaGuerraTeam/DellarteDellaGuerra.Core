using System;
using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.PoliticalMap.Labels;
using DellarteDellaGuerra.Domain.Titles.Model;
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
    /// <item>Owners: every region uses its holder's kingdom's flat base map colour;
    /// a kingdomless clan's region uses a colour from the kingdomless palette.
    /// Region identities stay separate so gold frontiers still divide same-coloured domains.</item>
    /// </list>
    /// A settlement with no owner and land under no faction are left unfilled. The keys are not
    /// drawn; the region labels group by them. In the owners view the region key is the clan
    /// whose region the land is painted in.
    /// </remarks>
    public class PoliticalMapPainter
    {
        public const string SettlementTerritoryPrefix = "settlement:";

        public static string? SettlementIdFromKey(string key) =>
            key.StartsWith(SettlementTerritoryPrefix, StringComparison.Ordinal) ? key.Substring(SettlementTerritoryPrefix.Length) : null;

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

        /// <summary>Geographic labels follow territorial seats, independently of who owns their land.</summary>
        public string GeographicLabelKeyOf(string settlementId, IPoliticalMapWorld world, PoliticalMapOwnerLevel level)
        {
            string seat = world.SeatOf(settlementId) ?? settlementId;
            string? title = _feudalStructure.GetTitleIdBySeat(seat);
            if (title is null) return SettlementTerritoryPrefix + seat;
            // Far out shows the region directly under the realm; close in, the shire directly under that region.
            var seen = new HashSet<string> { title };
            string? belowTop = null;
            while (_feudalStructure.GetDeJureSuzerainTitleId(title) is { } parent
                && _feudalStructure.GetRank(parent) < TitleRank.King && seen.Add(parent))
            {
                belowTop = title;
                title = parent;
            }
            return level == PoliticalMapOwnerLevel.SubVassals && belowTop is not null ? belowTop : title;
        }

        public string? GeographicLabelNameOf(string key) =>
            _feudalStructure.GetTitleName(key);

        public TitleRank? GeographicLabelRankOf(string key) =>
            _feudalStructure.GetMapLabelRank(key);

        public PoliticalMapTitleDomains TitleDomainsOf(IPoliticalMapWorld world) =>
            new(_feudalStructure, world, HierarchyOf(world));

        public string? DomainLabelNameOf(string key, PoliticalMapOwnerLevel level = PoliticalMapOwnerLevel.Vassals) =>
            level == PoliticalMapOwnerLevel.SubVassals ? _feudalStructure.GetTitleName(key)
                : _titleRepository.GetTitle(key)?.Name ?? _feudalStructure.GetTitleName(key);

        public TitleRank? DomainLabelRankOf(string key, PoliticalMapOwnerLevel level = PoliticalMapOwnerLevel.Vassals) =>
            level == PoliticalMapOwnerLevel.SubVassals ? _feudalStructure.GetMapLabelRank(key)
                : _titleRepository.GetTitle(key)?.Rank ?? _feudalStructure.GetMapLabelRank(key);

        /// <summary>Shared identities for labels and click selection at the displayed zoom level.</summary>
        public string?[] LabelKeysOf(PoliticalMapView view, PoliticalMapLabelMode mode,
            IReadOnlyList<string> settlementIds, IReadOnlyList<PoliticalMapSettlementPaint> paints,
            IPoliticalMapWorld world, PoliticalMapOwnerLevel level)
        {
            if (settlementIds.Count != paints.Count) throw new ArgumentException("One paint per settlement is required.", nameof(paints));
            var keys = new string?[settlementIds.Count];
            PoliticalMapTitleDomains? domains = view == PoliticalMapView.Owners && mode == PoliticalMapLabelMode.Titles
                ? TitleDomainsOf(world) : null;
            for (int i = 0; i < keys.Length; i++)
                keys[i] = view switch
                {
                    PoliticalMapView.Kingdom => paints[i].RealmKey,
                    PoliticalMapView.DeJure => GeographicLabelKeyOf(settlementIds[i], world, level),
                    PoliticalMapView.Owners when mode == PoliticalMapLabelMode.Titles => domains!.KeyOf(settlementIds[i], level),
                    PoliticalMapView.Owners => paints[i].RegionKey,
                    _ => null
                };
            return keys;
        }

        /// <summary>Expands a local title selection when the clicked land joins its overview title.</summary>
        public PoliticalMapSelection PromoteSelection(PoliticalMapSelection selection, PoliticalMapOwnerLevel level,
            string settlementId, IPoliticalMapWorld world)
        {
            if (selection.Level != PoliticalMapOwnerLevel.SubVassals || level != PoliticalMapOwnerLevel.Vassals
                || selection.Mode != PoliticalMapLabelMode.Titles) return selection;
            string? key = selection.View switch
            {
                PoliticalMapView.Owners => TitleDomainsOf(world).KeyOf(settlementId, level),
                PoliticalMapView.DeJure => GeographicLabelKeyOf(settlementId, world, level),
                _ => null
            };
            return key is null ? selection : selection with { Key = key, Level = level };
        }

        /// <summary>Selects the actual title or clan domain, including disconnected vassal territories.</summary>
        public string?[] SelectionKeysOf(PoliticalMapSelection selection, IReadOnlyList<string> settlementIds,
            IReadOnlyList<PoliticalMapSettlementPaint> paints, IPoliticalMapWorld world)
        {
            string?[] keys = LabelKeysOf(selection.View, selection.Mode, settlementIds, paints, world, selection.Level);
            if (selection.View == PoliticalMapView.DeJure)
            {
                for (int i = 0; i < keys.Length; i++)
                {
                    string local = GeographicLabelKeyOf(settlementIds[i], world, PoliticalMapOwnerLevel.SubVassals);
                    var seen = new HashSet<string>(StringComparer.Ordinal);
                    bool contains = false;
                    for (string? current = local; current is not null && seen.Add(current);
                         current = _feudalStructure.GetDeJureSuzerainTitleId(current))
                        if (current == selection.Key) { contains = true; break; }
                    keys[i] = contains ? selection.Key : null;
                }
                return keys;
            }
            if (selection.View != PoliticalMapView.Owners) return keys;
            if (selection.Mode == PoliticalMapLabelMode.Titles && selection.Level == PoliticalMapOwnerLevel.SubVassals)
                return keys;
            PoliticalMapTitleDomains domains = TitleDomainsOf(world);
            PoliticalMapVassalHierarchy hierarchy = HierarchyOf(world);
            for (int i = 0; i < keys.Length; i++)
            {
                // A live title-mode key is selectable even when it names a settlement without a held title.
                if (selection.Mode == PoliticalMapLabelMode.Titles && keys[i] == selection.Key) continue;
                bool contains = selection.Mode == PoliticalMapLabelMode.Titles
                    ? domains.Contains(selection.Key, settlementIds[i])
                    : world.OwnerClanOf(settlementIds[i]) is { } owner && hierarchy.ChainOf(owner).Contains(selection.Key);
                // Never keep a label key that matched before a transfer but no longer belongs to this domain.
                keys[i] = contains ? selection.Key : keys[i] == selection.Key ? null : keys[i];
            }
            return keys;
        }

        public IReadOnlyDictionary<string, string> Fold(Grid.PoliticalMapGrid grid, IReadOnlyList<string?> territoryOfSettlement) =>
            PoliticalMapDemesneFold.Fold(grid, territoryOfSettlement, territory => _feudalStructure.GetRank(territory));

        /// <summary>
        /// The opaque swatch colour of every faction the legend lists for a view, by faction id, as
        /// last painted with its neighbours known (the overlay's paint). Kingdom view: each faction's
        /// map colour. Owners view: each kingdom's base colour (shared by all its clans) and each
        /// kingdomless clan's own colour.
        /// </summary>
        public IReadOnlyDictionary<string, uint> MapColours(PoliticalMapView view) =>
            _legendColours.TryGetValue(view, out IReadOnlyDictionary<string, uint>? colours) ? colours : NoColours;

        /// <param name="neighbours">
        /// The grid's touching settlement pairs (<see cref="PoliticalMapSettlementAdjacency"/>), so
        /// touching kingdomless regions get different colours; null when only the keys matter.
        /// </param>
        /// <param name="level">How deep the owners view splits each kingdom; the kingdom view ignores it.</param>
        public PoliticalMapSettlementPaint[] Paint(
            PoliticalMapView view,
            IReadOnlyList<string> settlementIds,
            IReadOnlyList<string> territoryOfSettlement,
            IReadOnlyDictionary<string, string> folds,
            IPoliticalMapWorld world,
            IReadOnlyList<(int A, int B)>? neighbours = null,
            PoliticalMapOwnerLevel level = PoliticalMapOwnerLevel.SubVassals,
            PoliticalMapLabelMode mode = PoliticalMapLabelMode.Clans)
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
            var parentKeys = new string?[count];
            for (int index = 0; index < count; index++)
            {
                string territory = territoryOfSettlement[index];
                keys[index] = (folds.TryGetValue(territory, out string? folded) ? folded : territory, null, null);
            }

            if (view == PoliticalMapView.DeJure)
            {
                for (int index = 0; index < count; index++)
                {
                    string key = GeographicLabelKeyOf(settlementIds[index], world, level);
                    regionKeys[index] = key;
                    keys[index] = (key, null, null);
                    parentKeys[index] = level == PoliticalMapOwnerLevel.SubVassals
                        ? GeographicLabelKeyOf(settlementIds[index], world, PoliticalMapOwnerLevel.Vassals) : null;
                }
                var colours = PoliticalMapColouring.Assign(Weights(regionKeys), Touching(regionKeys, neighbours),
                    _ => PoliticalMapColouring.KingdomlessPalette, NoColours, null, PoliticalMapColouring.DistinctMinDistance);
                Fill(regionKeys, colours, fills);
                _legendColours[view] = NoColours;
            }
            else if (view == PoliticalMapView.Kingdom) PaintKingdoms(settlementIds, world, neighbours, remember, fills, keys);
            else PaintOwners(settlementIds, world, neighbours, remember, level, fills, regionKeys, parentKeys);

            if (view == PoliticalMapView.Owners && mode == PoliticalMapLabelMode.Titles)
            {
                PoliticalMapTitleDomains domains = TitleDomainsOf(world);
                for (int index = 0; index < count; index++)
                {
                    regionKeys[index] = domains.KeyOf(settlementIds[index], level);
                    parentKeys[index] = level == PoliticalMapOwnerLevel.SubVassals
                        ? domains.KeyOf(settlementIds[index], PoliticalMapOwnerLevel.Vassals) : null;
                }
            }

            for (int index = 0; index < count; index++)
            {
                paints[index] = new PoliticalMapSettlementPaint(
                    PoliticalMapPalette.Wash(fills[index]), 0,
                    keys[index].County, keys[index].Duchy, keys[index].Realm, regionKeys[index])
                { ParentRegionKey = parentKeys[index] };
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

        // Kingdom regions share their base colour; kingdomless clans use the kingdomless palette.
        private void PaintOwners(
            IReadOnlyList<string> settlementIds, IPoliticalMapWorld world, IReadOnlyList<(int A, int B)> neighbours, bool remember,
            PoliticalMapOwnerLevel level, uint?[] fills, string?[] regionKeys, string?[] parentKeys)
        {
            PoliticalMapVassalHierarchy hierarchy = HierarchyOf(world);
            var holders = new Dictionary<string, string>(StringComparer.Ordinal);
            var parentRegions = new string?[settlementIds.Count];
            for (int index = 0; index < regionKeys.Length; index++)
            {
                if (world.OwnerClanOf(settlementIds[index]) is not { } owner) continue;
                if (!holders.TryGetValue(owner, out string? holder)) holders[owner] = holder = hierarchy.RegionHolderOf(owner, level);
                regionKeys[index] = holder;
                parentRegions[index] = hierarchy.RegionHolderOf(owner, PoliticalMapOwnerLevel.Vassals);
                if (level == PoliticalMapOwnerLevel.SubVassals) parentKeys[index] = parentRegions[index];
            }

            var kingdomBase = new Dictionary<string, uint>();
            var kingdomOfHolder = new Dictionary<string, string>();
            foreach (string holder in holders.Values.Concat(parentRegions.OfType<string>()).Distinct())
            {
                if (world.KingdomOfClan(holder) is not { } kingdom) continue;
                if (!kingdomBase.ContainsKey(kingdom))
                {
                    if (PoliticalMapPalette.KingdomColor(kingdom, world.FactionColor(kingdom)) is not { } colour) continue;
                    kingdomBase[kingdom] = colour | 0xFF000000u;
                }

                kingdomOfHolder[holder] = kingdom;
            }

            // Always assign the parent level first, regardless of which level was requested first.
            var fixedColours = kingdomOfHolder.ToDictionary(pair => pair.Key, pair => kingdomBase[pair.Value]);
            const string parentTier = "Owners:Vassals";
            IReadOnlyDictionary<string, uint> parentColours = PoliticalMapColouring.Assign(
                Weights(parentRegions), Touching(parentRegions, neighbours), _ => PoliticalMapColouring.KingdomlessPalette,
                fixedColours, Previous(parentTier, remember), PoliticalMapColouring.DistinctMinDistance);
            if (remember) _previous[parentTier] = parentColours;

            string tier = "Owners:" + level;
            IReadOnlyDictionary<string, uint> colours = parentColours;
            if (level == PoliticalMapOwnerLevel.SubVassals)
            {
                Dictionary<string, int> weights = Weights(regionKeys);
                var kept = parentColours.Where(pair => weights.ContainsKey(pair.Key))
                    .ToDictionary(pair => pair.Key, pair => pair.Value);
                colours = PoliticalMapColouring.Assign(weights, Touching(regionKeys, neighbours), _ => PoliticalMapColouring.KingdomlessPalette,
                    kept, Previous(tier, remember), PoliticalMapColouring.DistinctMinDistance);
            }
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
