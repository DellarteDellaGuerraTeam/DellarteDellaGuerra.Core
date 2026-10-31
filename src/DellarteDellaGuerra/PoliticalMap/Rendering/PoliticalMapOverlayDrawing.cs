using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Domain.PoliticalMap;
using DellarteDellaGuerra.Domain.PoliticalMap.Grid;
using DellarteDellaGuerra.Domain.PoliticalMap.Overlay;

namespace DellarteDellaGuerra.PoliticalMap.Rendering
{
    /// <summary>
    /// The political map drawn on one campaign map scene: works out what each settlement should be
    /// painted, marks the territories whose paint changed in the shared scheduler, and feeds the
    /// scheduler's chunks to the renderer within a per-frame budget.
    /// </summary>
    public sealed class PoliticalMapOverlayDrawing
    {
        private const double FrameBudgetMs = 6.0;

        private readonly PoliticalMapPainter _painter;
        private readonly PoliticalMapRefreshScheduler _scheduler;
        private readonly IPoliticalMapWorld _world;
        private readonly PoliticalMapGrid _grid;
        private readonly string[] _territories;
        private readonly PoliticalMapMesher _mesher;
        private readonly IReadOnlyList<(int A, int B)> _neighbours;
        private readonly PoliticalMapRenderer _renderer;
        private readonly ILogger _logger;
        private readonly Dictionary<string, IReadOnlyList<PoliticalMapMeshChunk>> _chunksInBuild = new();
        private PoliticalMapSettlementPaint[] _paints = new PoliticalMapSettlementPaint[0];

        // The rebuild in flight, timed from its first chunk to its last, to log what a view switch costs.
        private Stopwatch? _rebuild;
        private int _rebuildFrames;
        private int _rebuildChunks;
        private double _rebuildSlowestFrameMs;

        public PoliticalMapOverlayDrawing(
            TaleWorlds.Engine.Scene scene,
            PoliticalMapGrid grid,
            PoliticalMapSheet sheet,
            PoliticalMapPainter painter,
            PoliticalMapRefreshScheduler scheduler,
            IPoliticalMapWorld world,
            ILogger logger)
        {
            _painter = painter;
            _scheduler = scheduler;
            _world = world;
            _grid = grid;
            _territories = grid.SettlementIds.Select(id => painter.TerritoryOf(id, world)).ToArray();
            Folds = painter.Fold(grid, _territories);
            _mesher = new PoliticalMapMesher(grid, _territories);
            _neighbours = PoliticalMapSettlementAdjacency.Of(grid);
            _renderer = new PoliticalMapRenderer(scene, grid, sheet);
            _logger = logger;
        }

        public TaleWorlds.Engine.Scene Scene => _renderer.Scene;

        public IReadOnlyDictionary<string, string> Folds { get; }

        public int TerritoryCount => _mesher.Territories.Count();

        public PoliticalMapGrid Grid => _grid;

        public void SetVisibility(PoliticalMapZoomBand band, bool visible) => _renderer.SetVisibility(band, visible);

        /// <summary>
        /// Recomputes every settlement's paint, the owners view at <paramref name="level"/>, and
        /// marks what changed since it was drawn.
        /// </summary>
        public void Repaint(PoliticalMapView view, PoliticalMapOwnerLevel level)
        {
            PoliticalMapSettlementPaint[] paints = _painter.Paint(view, _grid.SettlementIds, _territories, Folds, _world, _neighbours, level);
            var dirty = new HashSet<string>();
            for (int index = 0; index < paints.Length; index++)
            {
                PoliticalMapSettlementPaint? old = index < _paints.Length ? _paints[index] : null;
                PoliticalMapSettlementPaint paint = paints[index];
                // Only the colours are meshed: a key change alone redraws nothing.
                if (old is not null && old.Fill == paint.Fill && old.Hatch == paint.Hatch) continue;

                dirty.Add(_territories[index]);
            }

            _paints = paints;
            _scheduler.MarkDirty(dirty);
        }

        /// <summary>Builds the scheduled chunks for at most <see cref="FrameBudgetMs"/>.</summary>
        public void Drain(double clock)
        {
            if (!_scheduler.HasWork)
            {
                if (_rebuild is not null) LogRebuild();
                return;
            }

            _rebuild ??= Stopwatch.StartNew();
            _rebuildFrames++;
            var budget = Stopwatch.StartNew();
            while (budget.Elapsed.TotalMilliseconds < FrameBudgetMs)
            {
                IReadOnlyList<PoliticalMapChunk> chunks = _scheduler.Tick(clock, StartBuild);
                if (chunks.Count == 0) break;

                _rebuildChunks += chunks.Count;
                foreach (PoliticalMapChunk chunk in chunks)
                {
                    if (!_chunksInBuild.TryGetValue(chunk.TerritoryId, out IReadOnlyList<PoliticalMapMeshChunk>? meshChunks)) continue;

                    _renderer.AddChunk(chunk.TerritoryId, chunk.ChunkIndex, meshChunks[chunk.ChunkIndex]);
                    if (chunk.ChunkIndex == chunk.ChunkCount - 1)
                    {
                        _renderer.Commit(chunk.TerritoryId);
                        _chunksInBuild.Remove(chunk.TerritoryId);
                    }
                }
            }

            _rebuildSlowestFrameMs = Math.Max(_rebuildSlowestFrameMs, budget.Elapsed.TotalMilliseconds);
        }

        private void LogRebuild()
        {
            _logger.Info(
                $"Political map redrawn: {_rebuildChunks} chunks in {_rebuild!.ElapsedMilliseconds} ms over {_rebuildFrames} frames, "
                + $"slowest frame {_rebuildSlowestFrameMs:F1} ms.");
            _rebuild = null;
            _rebuildFrames = 0;
            _rebuildChunks = 0;
            _rebuildSlowestFrameMs = 0;
        }

        // Asked once as a territory's rebuild starts: plan its chunks from the paints of this moment.
        private int StartBuild(string territory)
        {
            if (_paints.Length == 0) return 0;

            IReadOnlyList<PoliticalMapMeshChunk> chunks = _mesher.Build(territory, _paints);
            _chunksInBuild[territory] = chunks;
            return chunks.Count;
        }
    }
}
