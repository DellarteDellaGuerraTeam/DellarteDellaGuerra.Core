using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Domain.PoliticalMap.Grid;
using DellarteDellaGuerra.Domain.PoliticalMap.Labels;
using DellarteDellaGuerra.Domain.PoliticalMap.Overlay;
using DellarteDellaGuerra.Domain.Titles.Port;
using DellarteDellaGuerra.PoliticalMap.Labels;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.PoliticalMap.Api.Campaign
{
    /**
     * <summary>
     *  Names the political map's regions on the campaign map while a view is drawn, in place of
     *  the settlement nameplates: kingdoms in the kingdom view, the vassal-hierarchy regions in the
     *  owners view, one label per region at the owners view's current level
     *  (<see cref="PoliticalMapOverlayCampaignBehavior.DisplayedOwnerLevel"/>). The owners view can switch
     *  its labels between the holder's highest title and the holder clan (<see cref="Modes"/>):
     *  only the text changes.
     * </summary>
     * <remarks>
     *  <para>
     *  Once a second, and as soon as the view or the owners level changes, every settlement's key
     *  in each of the view's label tiers is recomputed (cheap: the painter's keys, no geometry) and
     *  compared with the three cached layouts; only a cache miss finds regions again
     *  (<see cref="PoliticalMapRegions"/>), on a worker thread. Once current labels are ready,
     *  missing Owners levels are warmed on that worker. The names and the terrain height
     *  under each pole are then read on the main thread. Labels are shown only while they were
     *  built for the view and level drawn, so the two levels never mix and none go stale.
     *  </para>
     *  <para>
     *  Every frame each label is projected, sized to how big its region shows on screen (never to
     *  its name's length: a long name may run over its neighbours) and placed widest tier and
     *  largest region first, a label that would overlap one already placed left out, and so is
     *  one naming the same thing as a label placed nearby (<see cref="PoliticalMapLabelLayout"/>).
     *  </para>
     *  <para>
     *  The labels are a Gauntlet layer just under the nameplate layer, so party nameplates draw
     *  over them; it takes no input. While a view is drawn the settlement nameplates are held back
     *  (<see cref="PoliticalMapNameplates"/>); turning the view off brings them back.
     *  </para>
     * </remarks>
     */
    public class PoliticalMapLabelsCampaignBehavior : CampaignBehaviorBase
    {
        public const string MovieName = "PoliticalMapLabels";

        private const float RecomputeIntervalSeconds = 1f;
        // Just under the map's nameplate layer (90).
        private const int LayerOrder = 85;
        // Gauntlet's reference height: a font size of 20 is 20 pixels on a 1080-line screen.
        private const float ReferenceHeight = 1080f;
        // Labels float a little over the terrain so hills do not hide them.
        private const float HeightOffset = 0.5f;

        private readonly PoliticalMapGridCampaignBehavior _gridBehavior;
        private readonly PoliticalMapOverlayCampaignBehavior _overlay;
        private readonly PoliticalMapPainter _painter;
        private readonly IFeudalStructure _feudalStructure;
        private readonly ILogger _logger;
        private readonly BannerlordPoliticalMapWorld _world = new();

        private PoliticalMapGrid? _grid;
        private string[] _territories = Array.Empty<string>();
        private IReadOnlyDictionary<string, string> _folds = new Dictionary<string, string>();

        private MapScreen? _screen;
        private GauntletLayer? _layer;
        private PoliticalMapLabelsVM? _vm;

        private PoliticalMapView _keysView = PoliticalMapView.Off;
        private PoliticalMapOwnerLevel _keysLevel;
        private readonly PoliticalMapLabelRegionCache _regionCache = new();
        private Task<IReadOnlyList<PoliticalMapRegion>[]>? _finding;
        private CancellationTokenSource? _findingCancellation;
        private PoliticalMapView _findingView;
        private PoliticalMapOwnerLevel _findingLevel;
        private string?[][] _findingKeys = Array.Empty<string?[]>();
        private IReadOnlyList<PoliticalMapRegion>[]? _regionsByTier;
        private IReadOnlyList<PoliticalMapLabelTier> _regionTiers = Array.Empty<PoliticalMapLabelTier>();
        private bool _renameNow;
        private readonly List<Label> _labels = new();
        private readonly Dictionary<(PoliticalMapLabelTier Tier, string Key), string?> _resolvedNames = new();
        private readonly List<PoliticalMapLabelCandidate> _candidates = new();
        private float _sinceRecompute;
        private bool _failed;

        public PoliticalMapLabelsCampaignBehavior(
            PoliticalMapGridCampaignBehavior gridBehavior,
            PoliticalMapOverlayCampaignBehavior overlay,
            PoliticalMapPainter painter,
            IFeudalStructure feudalStructure,
            ILoggerFactory loggerFactory)
        {
            _gridBehavior = gridBehavior;
            _overlay = overlay;
            _painter = painter;
            _feudalStructure = feudalStructure;
            _logger = loggerFactory.CreateLogger<PoliticalMapLabelsCampaignBehavior>();
            Modes.Changed += () => _renameNow = true;
        }

        /// <summary>
        /// What the owners view's labels name, titles or clans; for the map bar's label row and
        /// <c>polmap.labels</c>. Session only, not saved.
        /// </summary>
        public PoliticalMapLabelModes Modes { get; } = new();

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, _ => Reset());
        }

        public override void SyncData(IDataStore dataStore)
        {
        }

        /// <summary>Called once a frame, after the overlay's tick, with the real time since the last frame.</summary>
        public void Tick(float realDt)
        {
            MapScreen? mapScreen = MapScreen.Instance;
            if (_failed || mapScreen?.MapScene is null)
            {
                PoliticalMapNameplates.HideSettlementNameplates = false;
                return;
            }

            try
            {
                TickOnMap(mapScreen, realDt);
            }
            catch (Exception exception)
            {
                _logger.Error("Political map labels failed; they stay off, with the settlement nameplates back, until restart.", exception);
                _failed = true;
                PoliticalMapNameplates.HideSettlementNameplates = false;
                if (_vm is not null) _vm.IsVisible = false;
            }
        }

        private void TickOnMap(MapScreen mapScreen, float realDt)
        {
            PoliticalMapGrid? grid = _gridBehavior.Grid;
            if (grid is null)
            {
                PoliticalMapNameplates.HideSettlementNameplates = false;
                return;
            }

            if (grid != _grid) Initialise(grid);
            if (_screen != mapScreen) AttachTo(mapScreen);

            PoliticalMapView view = _overlay.DisplayedView;
            PoliticalMapOwnerLevel level = LevelOf(view);
            PoliticalMapNameplates.HideSettlementNameplates = view != PoliticalMapView.Off;

            bool completed = false;
            if (_finding is { IsCompleted: true } finding)
            {
                _finding = null;
                _findingCancellation!.Dispose();
                _findingCancellation = null;
                if (finding.Status == TaskStatus.RanToCompletion)
                {
                    _regionCache.Store(_findingView, _findingLevel, _findingKeys, finding.Result);
                }
                else if (finding.Exception is not null) throw finding.Exception;
                _findingKeys = Array.Empty<string?[]>();
                completed = true;
            }

            _sinceRecompute += realDt;
            if (completed || _renameNow || view != _keysView || level != _keysLevel || _sinceRecompute >= RecomputeIntervalSeconds)
            {
                _sinceRecompute = 0f;
                RecomputeKeys(mapScreen, view, level);
                if (view != PoliticalMapView.Off && _regionsByTier is not null && _finding is null)
                    PrecomputeOwnerLevels(view == PoliticalMapView.Owners ? OtherOwnerLevel(level) : _overlay.OwnerLevel,
                        view == PoliticalMapView.Kingdom);
            }

            _renameNow = false;

            bool shown = view != PoliticalMapView.Off
                         && _keysView == view
                         && _keysLevel == level
                         && _regionsByTier is not null
                         && TaleWorlds.CampaignSystem.Campaign.Current?.ConversationManager?.IsConversationInProgress != true;
            _vm!.IsVisible = shown;
            if (shown) Layout(mapScreen);
        }

        private void Reset()
        {
            // The old map screen's layer went with it: drop the references without touching them.
            _grid = null;
            _screen = null;
            _layer = null;
            _vm = null;
            CancelFinding();
            _regionCache.Clear();
            _regionsByTier = null;
            _keysView = PoliticalMapView.Off;
            _sinceRecompute = RecomputeIntervalSeconds;
            _labels.Clear();
            _resolvedNames.Clear();
            PoliticalMapNameplates.HideSettlementNameplates = false;
        }

        private void Initialise(PoliticalMapGrid grid)
        {
            _grid = grid;
            _territories = grid.SettlementIds.Select(id => _painter.TerritoryOf(id, _world)).ToArray();
            _folds = _painter.Fold(grid, _territories);
            CancelFinding();
            _regionCache.Clear();
            _regionsByTier = null;
            _keysView = PoliticalMapView.Off;
            _sinceRecompute = RecomputeIntervalSeconds;
            _vm?.Labels.Clear();
            _labels.Clear();
            _resolvedNames.Clear();
        }

        private void AttachTo(MapScreen mapScreen)
        {
            _screen = mapScreen;
            _vm = new PoliticalMapLabelsVM();
            _layer = new GauntletLayer("DadgPoliticalMapLabels", LayerOrder);
            _layer.LoadMovie(MovieName, _vm);
            mapScreen.AddLayer(_layer);
            // The labels belonged to the old screen's movie: build them again.
            _labels.Clear();
            _regionsByTier = null;
            _keysView = PoliticalMapView.Off;
            _sinceRecompute = RecomputeIntervalSeconds;
        }

        // The level the labels group the owners view by; the other views have one level.
        private PoliticalMapOwnerLevel LevelOf(PoliticalMapView view) =>
            view == PoliticalMapView.Owners ? _overlay.DisplayedOwnerLevel : PoliticalMapOwnerLevel.SubVassals;

        private void RecomputeKeys(MapScreen mapScreen, PoliticalMapView view, PoliticalMapOwnerLevel level)
        {
            _keysView = view;
            _keysLevel = level;
            if (view == PoliticalMapView.Off)
            {
                CancelFinding();
                _regionsByTier = null;
                return;
            }

            IReadOnlyList<PoliticalMapLabelTier> tiers = PoliticalMapLabelTiers.Of(view);
            string?[][] keys = KeysOf(view, level);
            _regionsByTier = _regionCache.Get(view, level, keys);
            if (_regionsByTier is not null)
            {
                _regionTiers = tiers;
                // Names can change without changing any region key (titles, clans, localisation).
                BuildLabels(mapScreen, view, level);
                return;
            }

            if (_finding is not null && _findingView == view && _findingLevel == level
                && PoliticalMapLabelRegionCache.SameKeys(keys, _findingKeys)) return;

            StartFinding(view, level, keys);
        }

        private string?[][] KeysOf(PoliticalMapView view, PoliticalMapOwnerLevel level)
        {
            PoliticalMapGrid grid = _grid!;
            IReadOnlyList<PoliticalMapLabelTier> tiers = PoliticalMapLabelTiers.Of(view);
            IReadOnlyList<string?>? ownerRegionKeys = tiers.Contains(PoliticalMapLabelTier.Owner) ? OwnerRegionKeys(grid, level) : null;
            return tiers.Select(tier => PoliticalMapLabelTiers.KeysOf(tier, grid.SettlementIds, ownerRegionKeys, _world)).ToArray();
        }

        private static PoliticalMapOwnerLevel OtherOwnerLevel(PoliticalMapOwnerLevel level) =>
            level == PoliticalMapOwnerLevel.Vassals ? PoliticalMapOwnerLevel.SubVassals : PoliticalMapOwnerLevel.Vassals;

        private void PrecomputeOwnerLevels(PoliticalMapOwnerLevel first, bool includeOther)
        {
            string?[][] keys = KeysOf(PoliticalMapView.Owners, first);
            if (_regionCache.Get(PoliticalMapView.Owners, first, keys) is null)
                StartFinding(PoliticalMapView.Owners, first, keys);
            else if (includeOther)
                PrecomputeOwnerLevels(OtherOwnerLevel(first), false);
        }

        private void StartFinding(PoliticalMapView view, PoliticalMapOwnerLevel level, string?[][] keys)
        {
            // A foreground miss must not wait for an obsolete find or a speculative other level.
            CancelFinding();
            PoliticalMapGrid grid = _grid!;
            IReadOnlyList<PoliticalMapLabelTier> tiers = PoliticalMapLabelTiers.Of(view);
            _findingView = view;
            _findingLevel = level;
            _findingKeys = keys;
            _findingCancellation = new CancellationTokenSource();
            CancellationToken cancellation = _findingCancellation.Token;
            _finding = Task.Run(() =>
            {
                var regions = new IReadOnlyList<PoliticalMapRegion>[keys.Length];
                for (int index = 0; index < keys.Length; index++)
                {
                    cancellation.ThrowIfCancellationRequested();
                    regions[index] = PoliticalMapRegions.Find(grid, keys[index], PoliticalMapLabelTiers.MinCells(tiers[index]));
                }
                cancellation.ThrowIfCancellationRequested();
                return regions;
            }, cancellation);
        }

        private void CancelFinding()
        {
            if (_finding is null) return;
            _findingCancellation!.Cancel();
            _ = _finding.ContinueWith(task => { _ = task.Exception; }, CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            _finding = null;
            _findingCancellation.Dispose();
            _findingCancellation = null;
            _findingKeys = Array.Empty<string?[]>();
        }

        // The owners view's own regions at a level (each the clan they belong to), so labels group as the colours do.
        private IReadOnlyList<string?> OwnerRegionKeys(PoliticalMapGrid grid, PoliticalMapOwnerLevel level) =>
            _painter.Paint(PoliticalMapView.Owners, grid.SettlementIds, _territories, _folds, _world, null, level)
                .Select(paint => paint.RegionKey)
                .ToArray();

        private void BuildLabels(MapScreen mapScreen, PoliticalMapView view, PoliticalMapOwnerLevel level)
        {
            IReadOnlyList<PoliticalMapRegion>[] regionsByTier = _regionsByTier!;
            PoliticalMapLabelMode mode = Modes.Of(view);
            PoliticalMapVassalHierarchy? hierarchy = view == PoliticalMapView.Owners && mode == PoliticalMapLabelMode.Titles
                ? _painter.HierarchyOf(_world) : null;
            _resolvedNames.Clear();
            int labelIndex = 0;
            bool changed = false;
            for (int index = 0; index < regionsByTier.Length; index++)
            {
                PoliticalMapLabelTier tier = _regionTiers[index];
                foreach (PoliticalMapRegion region in regionsByTier[index])
                {
                    var key = (tier, region.Key);
                    if (!_resolvedNames.TryGetValue(key, out string? name))
                        _resolvedNames[key] = name = NameOf(tier, mode, region.Key, hierarchy);
                    if (string.IsNullOrWhiteSpace(name)) continue;

                    if (labelIndex >= _labels.Count || !ReferenceEquals(_labels[labelIndex].Region, region)
                        || _labels[labelIndex].Tier != tier || _labels[labelIndex].Text != name) changed = true;
                    labelIndex++;
                }
            }

            // Live names are cheap to check; leave the Gauntlet collection and terrain reads alone when unchanged.
            if (!changed && labelIndex == _labels.Count) return;

            _labels.Clear();
            _vm!.Labels.Clear();
            for (int index = 0; index < regionsByTier.Length; index++)
            {
                PoliticalMapLabelTier tier = _regionTiers[index];
                foreach (PoliticalMapRegion region in regionsByTier[index])
                {
                    string? name = _resolvedNames[(tier, region.Key)];
                    if (string.IsNullOrWhiteSpace(name)) continue;

                    float height = mapScreen.MapScene.GetTerrainHeight(new Vec2(region.X, region.Y)) + HeightOffset;
                    var vm = new PoliticalMapLabelVM(name!);
                    _labels.Add(new Label(tier, region, new Vec3(region.X, region.Y, height), name!, vm));
                    _vm.Labels.Add(vm);
                }
            }

            _keysView = view;
            _keysLevel = level;
        }

        // A region's label in a mode. An owners region is named after its holder clan, or its
        // holder's highest title (a ruler's: the realm's); a clan holding no title keeps its name.
        private string? NameOf(PoliticalMapLabelTier tier, PoliticalMapLabelMode mode, string key, PoliticalMapVassalHierarchy? hierarchy)
        {
            switch (tier)
            {
                case PoliticalMapLabelTier.Faction:
                    return TaleWorlds.CampaignSystem.Campaign.Current?.CampaignObjectManager.Find<Kingdom>(key)?.Name?.ToString()
                           ?? ClanNameOf(key);
                case PoliticalMapLabelTier.Owner:
                    return mode == PoliticalMapLabelMode.Titles
                        ? TitleNameOf(hierarchy?.HighestTitleOf(key)?.Id) ?? ClanNameOf(key)
                        : ClanNameOf(key);
                default:
                    return null;
            }
        }

        private static string? ClanNameOf(string? clanId) =>
            clanId is null ? null : TaleWorlds.CampaignSystem.Campaign.Current?.CampaignObjectManager.Find<Clan>(clanId)?.Name?.ToString();

        private string? TitleNameOf(string? titleId) =>
            titleId is not null && _feudalStructure.GetTitleName(titleId) is { } name ? new TextObject(name).ToString() : null;

        private void Layout(MapScreen mapScreen)
        {
            Camera camera = mapScreen.MapCameraView.Camera;
            float scale = Screen.RealScreenResolutionHeight / ReferenceHeight;
            if (scale <= 0f) return;

            _candidates.Clear();
            for (int index = 0; index < _labels.Count; index++)
            {
                Label label = _labels[index];
                if (!Project(camera, label.Position, out float x, out float y)) continue;

                // How big the region's inscribed circle shows: the longer of its two half-spans on screen.
                float radius = label.Region.Radius;
                float pixelRadius = 0f;
                if (Project(camera, label.Position + new Vec3(radius, 0f, 0f), out float rx, out float ry))
                    pixelRadius = Math.Max(pixelRadius, (float)Math.Sqrt((rx - x) * (rx - x) + (ry - y) * (ry - y)));
                if (Project(camera, label.Position + new Vec3(0f, radius, 0f), out float ux, out float uy))
                    pixelRadius = Math.Max(pixelRadius, (float)Math.Sqrt((ux - x) * (ux - x) + (uy - y) * (uy - y)));

                long priority = ((long)label.Tier << 32) | (uint)label.Region.CellCount;
                _candidates.Add(new PoliticalMapLabelCandidate(
                    index, x / scale, y / scale, pixelRadius / scale, label.Text.Length, priority, label.Text));
            }

            IReadOnlyList<PoliticalMapPlacedLabel> placed = PoliticalMapLabelLayout.Place(
                _candidates, Screen.RealScreenResolutionWidth / scale, Screen.RealScreenResolutionHeight / scale);

            var shown = new bool[_labels.Count];
            foreach (PoliticalMapPlacedLabel label in placed)
            {
                shown[label.Id] = true;
                _labels[label.Id].ViewModel.Place(label.X * scale, label.Y * scale, label.SizeIndex);
            }

            for (int index = 0; index < _labels.Count; index++)
            {
                if (!shown[index]) _labels[index].ViewModel.Hide();
            }
        }

        private static bool Project(Camera camera, Vec3 position, out float x, out float y)
        {
            x = 0f;
            y = 0f;
            float w = 0f;
            MBWindowManager.WorldToScreenInsideUsableArea(camera, position, ref x, ref y, ref w);
            return w > 0f;
        }

        private sealed class Label
        {
            public Label(PoliticalMapLabelTier tier, PoliticalMapRegion region, Vec3 position, string text, PoliticalMapLabelVM viewModel)
            {
                Tier = tier;
                Region = region;
                Position = position;
                Text = text;
                ViewModel = viewModel;
            }

            public PoliticalMapLabelTier Tier { get; }
            public PoliticalMapRegion Region { get; }
            public Vec3 Position { get; }
            public string Text { get; }
            public PoliticalMapLabelVM ViewModel { get; }
        }
    }
}
