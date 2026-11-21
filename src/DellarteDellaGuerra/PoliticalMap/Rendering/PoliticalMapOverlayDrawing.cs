using System;
using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Domain.PoliticalMap;
using DellarteDellaGuerra.Domain.PoliticalMap.Grid;
using DellarteDellaGuerra.Domain.PoliticalMap.Labels;
using DellarteDellaGuerra.Domain.PoliticalMap.Overlay;

namespace DellarteDellaGuerra.PoliticalMap.Rendering
{
    /// <summary>
    /// Retains the kingdom map and both owner levels as scene-local mesh caches. Idle frames prepare
    /// the next level. A cached zoom switch changes visibility; paint changes rebuild only affected
    /// territories and their smoothing neighbours before that level can be activated again.
    /// </summary>
    public sealed class PoliticalMapOverlayDrawing
    {
        private const double FrameBudgetMs = 6.0;
        private const double PrewarmBudgetMs = 2.0;
        private readonly PoliticalMapPainter _painter;
        private readonly PoliticalMapRefreshScheduler _events;
        private readonly IPoliticalMapWorld _world;
        private readonly PoliticalMapLabelModes _labelModes;
        private readonly string[] _territories;
        private readonly PoliticalMapSheet _sheet;
        private readonly IReadOnlyList<(int A, int B)> _neighbours;
        private readonly IReadOnlyList<(int A, int B)> _boundaryNeighbours;
        private readonly ILogger _logger;
        private readonly Dictionary<(PoliticalMapView View, PoliticalMapOwnerLevel Level, PoliticalMapLabelMode Mode), PoliticalMapCachedLayer> _layers = new();
        private (PoliticalMapView View, PoliticalMapOwnerLevel Level, PoliticalMapLabelMode Mode)? _requested;
        private (PoliticalMapView View, PoliticalMapOwnerLevel Level, PoliticalMapLabelMode Mode)? _shown;
        private PoliticalMapOwnerLevel _cameraLevel;
        private string? _selectedSettlementId;
        private PoliticalMapZoomBand _band = PoliticalMapZoomBand.Mid;
        private bool _visible;
        private readonly Dictionary<(PoliticalMapView View, PoliticalMapOwnerLevel Level, PoliticalMapLabelMode Mode), double> _lastValidation = new();

        public PoliticalMapOverlayDrawing(TaleWorlds.Engine.Scene scene, PoliticalMapGrid grid, PoliticalMapSheet sheet,
            PoliticalMapPainter painter, PoliticalMapRefreshScheduler scheduler, IPoliticalMapWorld world, ILogger logger,
            PoliticalMapLabelModes labelModes)
        {
            Scene = scene;
            Grid = grid;
            _sheet = sheet;
            _painter = painter;
            _events = scheduler;
            _world = world;
            _labelModes = labelModes;
            _logger = logger;
            _territories = grid.SettlementIds.Select(id => painter.TerritoryOf(id, world)).ToArray();
            Folds = painter.Fold(grid, _territories);
            TerritoryCount = _territories.Distinct().Count();
            _neighbours = PoliticalMapSettlementAdjacency.Of(grid);
            _boundaryNeighbours = PoliticalMapSettlementAdjacency.Of(grid, includeCorners: true)
                .Concat(PoliticalMapSettlementAdjacency.InSharedBlocks(grid, PoliticalMapMesher.MaxQuadSide, PoliticalMapMesher.SmoothingReach))
                .Distinct().ToArray();
        }

        public TaleWorlds.Engine.Scene Scene { get; }
        public PoliticalMapGrid Grid { get; }
        public IReadOnlyDictionary<string, string> Folds { get; }
        public int TerritoryCount { get; }
        public PoliticalMapView DisplayedView => _visible && PoliticalMapZoom.ShowsWash(_band) && _shown is { } shown ? shown.View : PoliticalMapView.Off;
        public PoliticalMapOwnerLevel DisplayedOwnerLevel => _shown?.Level ?? _cameraLevel;
        public PoliticalMapLabelMode DisplayedLabelMode => _shown?.Mode ?? PoliticalMapLabelMode.Clans;
        public PoliticalMapSelection? Selection { get; private set; }

        public void ClearSelection()
        {
            if (Selection is null) return;
            Selection = null;
            _selectedSettlementId = null;
            RefreshSelectionLayers();
        }

        public (string SettlementId, string? Key)? HitTest(float x, float y) =>
            DisplayedView != PoliticalMapView.Off && _shown is { } shown ? _layers[shown].DisplayedFrontiers?.HitTest(x, y) : null;

        public void SelectAt(float x, float y)
        {
            if (DisplayedView == PoliticalMapView.Off || _shown is not { } shown) return;
            var hit = HitTest(x, y);
            PoliticalMapSelection? selection = hit?.Key is { } key ? new(shown.View, shown.Level, shown.Mode, key) : null;
            _selectedSettlementId = selection is null ? null : hit?.SettlementId;
            if (Selection == selection) return;
            Selection = selection;
            RefreshSelectionLayers();
        }

        private void RefreshSelectionLayers()
        {
            // Both zoom caches must forget the old highlight before either can be shown again.
            foreach (var key in _layers.Keys.ToArray()) Validate(key);
        }

        public void SetVisibility(PoliticalMapZoomBand band, bool visible)
        {
            _band = band;
            _visible = visible;
            if (_shown is { } shown) _layers[shown].SetVisibility(band, visible);
        }

        public void Repaint(PoliticalMapView view, PoliticalMapOwnerLevel level)
        {
            if (view == PoliticalMapView.Off) return;
            var requested = Key(view, level);
            if (Selection is { } selected && !selected.Matches(requested.View, requested.Level, _labelModes.Of(view)))
                ClearSelection();
            else if (Selection is { } local && _selectedSettlementId is { } settlement)
            {
                PoliticalMapSelection promoted = _painter.PromoteSelection(local, requested.Level, settlement, _world);
                if (promoted != local)
                {
                    Selection = promoted;
                    RefreshSelectionLayers();
                }
            }
            _cameraLevel = level;
            _requested = requested;
            Validate(_requested.Value);
            ShowIfReady();
        }

        public void Drain(double clock)
        {
            if (_requested is not { } requested) return;
            // Campaign events invalidate paints; each cached layer owns its separate upload queue.
            if (_events.HasWork)
            {
                _events.Clear();
                Validate(requested);
                _lastValidation.Clear();
            }
            if (!_visible) return;

            PoliticalMapCachedLayer layer = _layers[requested];
            if (!layer.IsReady)
            {
                layer.Drain(clock, FrameBudgetMs);
                ShowIfReady();
                return;
            }
            ShowIfReady();

            foreach (var candidate in WarmOrder(requested))
            {
                PoliticalMapCachedLayer warm = Layer(candidate);
                if (!_lastValidation.TryGetValue(candidate, out double last) || clock - last >= 1.0)
                {
                    Validate(candidate);
                    _lastValidation[candidate] = clock;
                }
                if (warm.IsReady) continue;
                warm.Drain(clock, PrewarmBudgetMs);
                break;
            }
        }

        private PoliticalMapSettlementPaint[] Paint((PoliticalMapView View, PoliticalMapOwnerLevel Level, PoliticalMapLabelMode Mode) key) =>
            _painter.Paint(key.View, Grid.SettlementIds, _territories, Folds, _world, _neighbours, key.Level, key.Mode);

        private void Validate((PoliticalMapView View, PoliticalMapOwnerLevel Level, PoliticalMapLabelMode Mode) key)
        {
            PoliticalMapSettlementPaint[] paints = Paint(key);
            if (Selection is { } selected && selected.Matches(key.View, key.Level, key.Mode))
                paints = selected.Apply(paints, _painter.SelectionKeysOf(selected, Grid.SettlementIds, paints, _world));
            Layer(key).Repaint(paints);
        }

        private PoliticalMapCachedLayer Layer((PoliticalMapView View, PoliticalMapOwnerLevel Level, PoliticalMapLabelMode Mode) key)
        {
            if (!_layers.TryGetValue(key, out PoliticalMapCachedLayer? layer))
                _layers[key] = layer = new PoliticalMapCachedLayer(Scene, Grid, _sheet, _territories, _boundaryNeighbours, _logger, $"{key.View}/{key.Level}/{key.Mode}");
            return layer;
        }

        private void ShowIfReady()
        {
            if (_requested is not { } requested || _shown == requested || !_layers[requested].IsReady) return;
            if (_shown is { } shown) _layers[shown].SetVisibility(_band, false);
            _shown = requested;
            _layers[requested].SetVisibility(_band, _visible);
        }

        private (PoliticalMapView View, PoliticalMapOwnerLevel Level, PoliticalMapLabelMode Mode) Key(PoliticalMapView view, PoliticalMapOwnerLevel level) =>
            (view, view == PoliticalMapView.Kingdom ? PoliticalMapOwnerLevel.SubVassals : level, _labelModes.Of(view));

        private IEnumerable<(PoliticalMapView View, PoliticalMapOwnerLevel Level, PoliticalMapLabelMode Mode)> WarmOrder((PoliticalMapView View, PoliticalMapOwnerLevel Level, PoliticalMapLabelMode Mode) requested)
        {
            PoliticalMapOwnerLevel opposite = _cameraLevel == PoliticalMapOwnerLevel.Vassals ? PoliticalMapOwnerLevel.SubVassals : PoliticalMapOwnerLevel.Vassals;
            if (requested.View == PoliticalMapView.DeJure)
            {
                yield return Key(PoliticalMapView.DeJure, opposite);
                yield break;
            }
            if (requested.View == PoliticalMapView.Kingdom) yield return Key(PoliticalMapView.Owners, _cameraLevel);
            yield return Key(PoliticalMapView.Owners, opposite);
            if (requested.View == PoliticalMapView.Owners) yield return Key(PoliticalMapView.Kingdom, _cameraLevel);
        }
    }
}
