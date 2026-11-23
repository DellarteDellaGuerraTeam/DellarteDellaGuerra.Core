using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Domain.PoliticalMap;
using DellarteDellaGuerra.Domain.PoliticalMap.Grid;
using DellarteDellaGuerra.Domain.PoliticalMap.Labels;
using DellarteDellaGuerra.Domain.PoliticalMap.Overlay;
using DellarteDellaGuerra.PoliticalMap.Rendering;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace DellarteDellaGuerra.PoliticalMap.Api.Campaign
{
    /**
     * <summary>
     *  Draws the political map over the campaign map once the settlement grid is ready, in the
     *  view the player picked, and keeps it up to date.
     * </summary>
     * <remarks>
     *  <para>
     *  Ticked every frame from the module's application tick, so it also runs while the campaign
     *  is paused. Nothing is rebuilt in an event handler: once a second the paint of every
     *  settlement is recomputed (cheap: title lookups, no meshes) and compared with what is drawn,
     *  and the territories that changed, with their neighbours when a border moved, are marked in
     *  cached map levels. Geometry planning runs on workers; engine uploads are budgeted over frames
     *  (<see cref="PoliticalMapOverlayDrawing"/>). Idle frames prepare the other levels, so a warmed
     *  view or zoom switch only changes visibility.
     *  </para>
     *  <para>
     *  The view and region selection are session-only; new and loaded campaigns start off.
     *  </para>
     *  <para>
     *  The owners view's level follows the camera (<see cref="PoliticalMapZoom.OwnerLevelFor"/>):
     *  the requested level is checked against live paints before its cached meshes are shown.
     *  The last completed level stays visible until a cold or invalidated level is ready.
     *  </para>
     *  <para>
     *  While a view is drawn it also keeps the map bar's legend (the factions holding land) and a
     *  hover readout of the land under the cursor, sampled a few times a second, and holds the map's
     *  snow back (<see cref="MapSnowSeasonFactor"/>). The fill opacity and the snow switch are
     *  session settings for the console (<c>polmap.opacity</c>, <c>polmap.snow</c>), not saved.
     *  </para>
     * </remarks>
     */
    public class PoliticalMapOverlayCampaignBehavior : CampaignBehaviorBase
    {
        private const float RepaintIntervalSeconds = 1f;
        private const float HoverIntervalSeconds = 0.2f;

        // The cursor ray's exclusions, as the map screen's own cursor uses.
        private const BodyFlags CursorRayExcludeFlags = (BodyFlags)79617;

        private readonly PoliticalMapGridCampaignBehavior _gridBehavior;
        private readonly PoliticalMapPainter _painter;
        private readonly PoliticalMapRefreshScheduler _scheduler;
        private readonly PoliticalMapHoverDescriber _hoverDescriber;
        private readonly ILogger _logger;
        private readonly BannerlordPoliticalMapWorld _world = new();
        private readonly PoliticalMapSnow _snow = new();

        private PoliticalMapView _view = PoliticalMapView.Off;
        private PoliticalMapOwnerLevel _ownerLevel = PoliticalMapOwnerLevel.SubVassals;

        private PoliticalMapOverlayDrawing? _drawing;
        private (TaleWorlds.Engine.Scene Scene, PoliticalMapGrid Grid)? _failedInitialisation;
        private double _clock;
        private float _sinceRepaint;
        private bool _repaintNow;
        private float _sinceHover;
        private IReadOnlyList<PoliticalMapLegendEntry> _legend = Array.Empty<PoliticalMapLegendEntry>();
        private PoliticalMapHoverInfo? _hover;

        public PoliticalMapOverlayCampaignBehavior(
            PoliticalMapGridCampaignBehavior gridBehavior,
            PoliticalMapPainter painter,
            PoliticalMapRefreshScheduler scheduler,
            PoliticalMapHoverDescriber hoverDescriber,
            ILoggerFactory loggerFactory)
        {
            _gridBehavior = gridBehavior;
            _painter = painter;
            _scheduler = scheduler;
            _hoverDescriber = hoverDescriber;
            _logger = loggerFactory.CreateLogger<PoliticalMapOverlayCampaignBehavior>();
            LabelModes.Changed += () => _repaintNow = true;
        }

        /// <summary>Raised when the view, the legend or the hover readout changes, for the map bar's panel.</summary>
        public event Action? Changed;

        public PoliticalMapView View => _view;

        /// <summary>How deep the owners view splits each kingdom at the camera's distance; the labels follow it.</summary>
        public PoliticalMapOwnerLevel OwnerLevel => _ownerLevel;

        /// <summary>The completed mesh level actually visible while a requested cold cache is preparing.</summary>
        public PoliticalMapView DisplayedView => _view == PoliticalMapView.Off ? PoliticalMapView.Off : _drawing?.DisplayedView ?? PoliticalMapView.Off;
        public PoliticalMapOwnerLevel DisplayedOwnerLevel => _drawing?.DisplayedOwnerLevel ?? _ownerLevel;
        public PoliticalMapLabelMode DisplayedLabelMode => _drawing?.DisplayedLabelMode ?? LabelModes.Of(_view);

        /// <summary>
        /// The factions holding land, in the current view's swatch colours (kingdom colours in the
        /// kingdom and owners views); empty until the overlay is drawn.
        /// </summary>
        public IReadOnlyList<PoliticalMapLegendEntry> Legend => _legend;

        /// <summary>The land under the cursor in the current view, or null when the cursor is off the painted map.</summary>
        public PoliticalMapHoverInfo? Hover => _hover;

        public PoliticalMapSnow Snow => _snow;
        public PoliticalMapLabelModes LabelModes { get; } = new();
        public PoliticalMapSelection? Selection => _drawing?.Selection;

        /// <summary>Consumes a native terrain click while the political map is displayed.</summary>
        public bool TrySelectAt(MapScreen mapScreen, float x, float y)
        {
            if (DisplayedView == PoliticalMapView.Off || ScreenManager.TopScreen != mapScreen
                || _drawing?.Scene != mapScreen.MapScene || mapScreen.IsInMenu
                || TaleWorlds.CampaignSystem.Campaign.Current?.ConversationManager?.IsConversationInProgress == true) return false;
            _drawing.SelectAt(x, y);
            Changed?.Invoke();
            return true;
        }

        public void SetView(PoliticalMapView view)
        {
            if (_view == view) return;
            _drawing?.ClearSelection();
            _view = view;
            _repaintNow = true;
            _sinceHover = HoverIntervalSeconds;
            _hover = null;
            Changed?.Invoke();
        }

        /// <summary>Sets the fill opacity (0 to 1) of every view; the whole map repaints at once.</summary>
        public void SetOpacity(float opacity)
        {
            PoliticalMapPalette.Opacity = opacity;
            _repaintNow = true;
        }

        /// <summary>
        /// The snow season factor the map scene should draw with, given the season's own: capped
        /// while a view is drawn on this map (see <see cref="PoliticalMapSnow"/>), the season's
        /// own otherwise, including before the overlay is ready and once its scene is gone.
        /// </summary>
        public float MapSnowSeasonFactor(float seasonFactor)
        {
            TaleWorlds.Engine.Scene? scene = MapScreen.Instance?.MapScene;
            return _drawing is not null && scene is not null && _drawing.Scene == scene
                ? _snow.SeasonFactorFor(_view, seasonFactor)
                : seasonFactor;
        }

        public void CycleView() => SetView(PoliticalMapViews.Next(_view));

        public override void RegisterEvents()
        {
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, _ => SetView(PoliticalMapView.Off));
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, _ => Reset());
        }

        public override void SyncData(IDataStore dataStore)
        {
            // The overlay is a session preference; loading a save always starts with the normal map.
            if (dataStore.IsLoading) SetView(PoliticalMapView.Off);
        }

        /// <summary>Called once a frame, paused or not, with the real time since the last frame.</summary>
        public void Tick(float realDt)
        {
            MapScreen? mapScreen = MapScreen.Instance;
            if (mapScreen?.MapScene is null) return;

            if (_drawing is null || _drawing.Scene != mapScreen.MapScene)
            {
                if (!TryInitialise(mapScreen)) return;
            }

            float cameraDistance = mapScreen.MapCameraView.CameraDistance;
            PoliticalMapOwnerLevel ownerLevel = PoliticalMapZoom.OwnerLevelFor(cameraDistance, _ownerLevel);
            if (ownerLevel != _ownerLevel)
            {
                _ownerLevel = ownerLevel;
                if (_view is PoliticalMapView.Owners or PoliticalMapView.DeJure)
                {
                    _repaintNow = true;
                    Changed?.Invoke();
                }
            }

            _clock += realDt;
            _sinceRepaint += realDt;
            if (_view != PoliticalMapView.Off && (_repaintNow || _sinceRepaint >= RepaintIntervalSeconds))
            {
                _sinceRepaint = 0f;
                _repaintNow = false;
                _drawing!.Repaint(_view, _ownerLevel);
                UpdateLegend();
            }

            _sinceHover += realDt;
            if (_sinceHover >= HoverIntervalSeconds)
            {
                _sinceHover = 0f;
                UpdateHover(mapScreen);
            }

            _drawing!.SetVisibility(PoliticalMapZoom.BandOf(cameraDistance), _view != PoliticalMapView.Off);
            _drawing.Drain(_clock);
        }

        private void Reset()
        {
            _view = PoliticalMapView.Off;
            // The old scene's entities went with it: drop the references without touching them.
            _drawing = null;
            _failedInitialisation = null;
            _legend = Array.Empty<PoliticalMapLegendEntry>();
            _hover = null;
            Changed?.Invoke();
        }

        private void UpdateLegend()
        {
            IReadOnlyList<PoliticalMapLegendEntry> legend = PoliticalMapLegend.KingdomEntries(
                _drawing!.Grid.SettlementIds, _world, _world, _painter.MapColours(_view));
            if (legend.SequenceEqual(_legend)) return;

            _legend = legend;
            Changed?.Invoke();
        }

        private void UpdateHover(MapScreen mapScreen)
        {
            PoliticalMapHoverInfo? hover = _view == PoliticalMapView.Off || ScreenManager.TopScreen != mapScreen ? null : DescribeCursor(mapScreen);
            if (Equals(hover, _hover)) return;

            _hover = hover;
            Changed?.Invoke();
        }

        private PoliticalMapHoverInfo? DescribeCursor(MapScreen mapScreen)
        {
            if (mapScreen.SceneLayer?.SceneView is not { } sceneView) return null;

            Vec3 near = Vec3.Zero;
            Vec3 far = Vec3.Zero;
            sceneView.TranslateMouse(ref near, ref far, -1f);
            if (!mapScreen.MapScene.RayCastForClosestEntityOrTerrain(near, far, out float _, out Vec3 point, 0.01f, CursorRayExcludeFlags)) return null;

            if (_drawing!.HitTest(point.x, point.y) is not { } hit) return null;
            return _hoverDescriber.Describe(DisplayedView, hit.SettlementId, _world, _world, DisplayedOwnerLevel);
        }

        private bool TryInitialise(MapScreen mapScreen)
        {
            if (_gridBehavior.Surface is not { } surface) return false;
            var (grid, sheet) = surface;
            if (_failedInitialisation is { } failed && failed.Scene == mapScreen.MapScene && failed.Grid == grid)
                return false;

            try
            {
                var clock = Stopwatch.StartNew();
                _drawing = new PoliticalMapOverlayDrawing(mapScreen.MapScene, grid, sheet, _painter, _scheduler, _world, _logger, LabelModes);
                _failedInitialisation = null;
                _repaintNow = true;
                _logger.Info(
                    $"Political map overlay ready: {_drawing.TerritoryCount} territories, "
                    + $"{_drawing.Folds.Count} demesnes folded ({string.Join(", ", _drawing.Folds.Select(fold => $"{fold.Key}->{fold.Value}"))}), "
                    + $"in {clock.ElapsedMilliseconds} ms.");
                return true;
            }
            catch (Exception exception)
            {
                _failedInitialisation = (mapScreen.MapScene, grid);
                _logger.Error("Could not set up the political map overlay; the map stays blank.", exception);
                _drawing = null;
                return false;
            }
        }

    }
}
